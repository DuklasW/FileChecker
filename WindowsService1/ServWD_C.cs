using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http.SelfHost;
using System.Web.Http;
using System.Net.Http;
using FileWatcherLibrary;
using System.Runtime.CompilerServices;
using System.IO;
using System.Collections;
using System.Configuration;

namespace WindowsService1
{ 
  
    public partial class ServWD_C : ServiceBase
    {
        private HttpSelfHostServer _server;

        public ServWD_C()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            EventLog eventLog;
            string eventSourceName = ConfigurationManager.AppSettings["eventSourceName"];//nazwa źródła
            string logName = ConfigurationManager.AppSettings["logName"]; //nazwa loga
            eventLog = new EventLog();

            if (!EventLog.SourceExists(eventSourceName))
            {
                EventLog.CreateEventSource(eventSourceName, logName);
            }
            try
            {
                var config = new HttpSelfHostConfiguration("http://localhost:8081/");

                config.Routes.MapHttpRoute(
                    name: "TextApi",
                    routeTemplate: "watcher/{controller}/{id}",
                    defaults: new { id = RouteParameter.Optional }
                );

                _server = new HttpSelfHostServer(config);
                _server.OpenAsync().Wait();
            }
            catch (Exception ex)
            {
                eventLog.WriteEntry(ex.Message, EventLogEntryType.Error);
            }
            
        }

        protected override void OnStop()
        {
            if (_server != null)
            {
                _server.CloseAsync().Wait();
                _server.Dispose();
            }
        }
    }
    public class MessageController : ApiController
    {
        private EventLog eventLog;

        [HttpPost]
        [Route("watcher/message")]
        public IHttpActionResult PostText(HttpRequestMessage request)
        {
            try
            {
                var content = request.Content.ReadAsStringAsync().Result;
                if (string.IsNullOrWhiteSpace(content))
                {
                    return BadRequest("Text cannot be empty.");
                }

                LogEvent(content);
                return Ok();
            }
            catch (Exception ex)
            {
                LogEvent(ex.Message);
                return InternalServerError(ex);
            }
        }

        private void LogEvent(string text)
        {
            string eventSourceName = ConfigurationManager.AppSettings["eventSourceName"];//nazwa źródła
            string logName = ConfigurationManager.AppSettings["logName"]; //nazwa loga
            eventLog = new EventLog();
            try
            {
                if (!EventLog.SourceExists(eventSourceName))
                {
                    EventLog.CreateEventSource(eventSourceName, logName);
                }
                eventLog.Source = eventSourceName;
                eventLog.Log = logName;
                string message = $"{text}";
                eventLog.WriteEntry(message, EventLogEntryType.Information);
            }
            catch (Exception ex)
            {
                string errorMessage = $"Error while logging event: {ex.Message}";
                eventLog.WriteEntry(errorMessage, EventLogEntryType.Error);
            }
        }

    }
    public class DirectoryController : ApiController
    {
        private EventLog eventLog;
        private FileWatcherManager _fileWatcherManager;

        [HttpPost]
        [Route("watcher/directory")]
        public IHttpActionResult PostText(HttpRequestMessage request)
        {
            try
            {
                var folderPath = request.Content.ReadAsStringAsync().Result;
                if (string.IsNullOrWhiteSpace(folderPath))
                {
                    return BadRequest("Folder path cannot be empty.");
                }
                if (folderPath.Equals("STOP", StringComparison.OrdinalIgnoreCase))
                {
                    LogEvent($"---Stopped watch---", true);
                    FileWatcherService.StopDirectoryWatcher();//zatrzymuje śledzenie folderu
                    return Ok("Watcher stopped.");
                }
                LogEvent($"---Start watch---\nDirectory: {folderPath}", true);
                _fileWatcherManager = FileWatcherService.GetDirectoryWatcherManager(folderPath);

                _fileWatcherManager.FileChangeEvent += (sender, e) =>
                {
                    
                    LogEvent(e, true);
                };

                return Ok("Watcher start!");
            }
            catch (Exception ex)
            {
                LogEvent(ex.Message, false);
                return InternalServerError(ex);
            }
        }


        private void LogEvent(string text, bool err)
        {
            string eventSourceName = ConfigurationManager.AppSettings["eventSourceNameDirectory"];//nazwa źródła
            string logName = ConfigurationManager.AppSettings["logName"]; //nazwa loga
            eventLog = new EventLog();
            try
            {
                if (!EventLog.SourceExists(eventSourceName))
                {
                    EventLog.CreateEventSource(eventSourceName, logName);
                }
                eventLog.Source = eventSourceName;
                eventLog.Log = logName;
                string message = $"{text}";
                if(!err) eventLog.WriteEntry(message, EventLogEntryType.Error);
                else eventLog.WriteEntry(message, EventLogEntryType.Information);
            }
            catch (Exception ex)
            {
                string errorMessage = $"Error while logging event: {ex.Message}";
                eventLog.WriteEntry(errorMessage, EventLogEntryType.Error);
            }
        }


    } 
    public class FileController : ApiController
    {
        private EventLog eventLog;
        private FileWatcherManager _fileWatcherManager;

        [HttpPost]
        [Route("watcher/file")]
        public IHttpActionResult PostText([FromBody] FileDataModel requestModel)
        {
            try
            {
                string filePath =  requestModel.FilePath;
                string fileName = requestModel.FileName;

                if (filePath.Equals("STOP", StringComparison.OrdinalIgnoreCase))
                {
                    LogEvent($"--- Stopped watch---", true);
                    FileWatcherService.StopFileWatcher();//zatrzymuje śledzenie folderu
                    return Ok("Watcher stopped.");
                }
                LogEvent($"--- Start watch---\nFile: {filePath}\\{fileName}", true);
                _fileWatcherManager = FileWatcherService.GetFileWatcherManager(filePath, fileName);
                _fileWatcherManager.FileContentChangedEvent += (sender, e) =>
                {
                    LogEvent(e, true);
                };
                return Ok("Watcher start!");
            }
            catch (Exception ex)
            {
                LogEvent(ex.Message, false);
                return InternalServerError(ex);
            }
        }

        private void LogEvent(string text, bool err)
        {
            string eventSourceName = ConfigurationManager.AppSettings["eventSourceNameFile"];//nazwa źródła
            string logName = ConfigurationManager.AppSettings["logName"]; //nazwa loga
            eventLog = new EventLog();
            try
            {
                if (!EventLog.SourceExists(eventSourceName))
                {
                    EventLog.CreateEventSource(eventSourceName, logName);
                }
                eventLog.Source = eventSourceName;
                eventLog.Log = logName;
                string message = $"{text}";
                if (!err) eventLog.WriteEntry(message, EventLogEntryType.Error);
                else eventLog.WriteEntry(message, EventLogEntryType.Information);
            }
            catch(Exception ex)
            {
                string errorMessage = $"Error while logging event: {ex.Message}";
                eventLog.WriteEntry(errorMessage, EventLogEntryType.Error);
            }
        }
    }


    public class StatusController : ApiController
    {
        private EventLog eventLog;

        [HttpGet]
        [Route("watcher/status")]
        public IHttpActionResult GetCurrentWatcherItem()
        {
            try
            {
                string watchedItemPath = FileWatcherService.GetCurrentWatcherItemPath();

                if (!string.IsNullOrEmpty(watchedItemPath))
                {
                    return Ok(watchedItemPath);
                }
                else return Ok("STOP");
            }
            catch (Exception ex)
            {
                LogEvent(ex.Message, false);
                return InternalServerError(ex);
            }
        }

        private void LogEvent(string text, bool err)
        {
            string eventSourceName = ConfigurationManager.AppSettings["eventSourceNameFile"];//nazwa źródła
            string logName = ConfigurationManager.AppSettings["logName"]; //nazwa loga
            eventLog = new EventLog();
            try
            {
                if (!EventLog.SourceExists(eventSourceName))
                {
                    EventLog.CreateEventSource(eventSourceName, logName);
                }
                eventLog.Source = eventSourceName;
                eventLog.Log = logName;
                string message = $"{text}";
                if (!err) eventLog.WriteEntry(message, EventLogEntryType.Error);
                else eventLog.WriteEntry(message, EventLogEntryType.Information);
            }
            catch (Exception ex)
            {
                string errorMessage = $"Error while logging event: {ex.Message}";
                eventLog.WriteEntry(errorMessage, EventLogEntryType.Error);
            }
        }
    }
}
