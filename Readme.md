#FileChecker


## Opis Projektu

Jest to solucja składająca się z 5 projektów:

- Aplikacja WPF (.NET Framework)
- Własna biblioteka (.NET Framework)
- Testy (NUnit)
- Usługa (Windows Service)
- Instalator

## Instalacja
1. Skompiluj projekt w środowisku Visual Studio.
2. Uruchom projekt instalatora, aby zainstalować usługę na systemie Windows.

## Funkcjonalności:

- Włączanie lub wyłączanie usługi obsługującej aplikację.
- Wybieranie folderu lub pliku do "śledzenia" (watcher), czyli ustawienie na niego monitora.
- Włączanie lub wyłączanie monitora dla plików.

## Jak to działa:

- Śledzenie odbywa się dwuetapowo: na ekranie aplikacji wyświetlają się zmiany, ale także do usługi zostaje wysłane na odpowiedni endpoint zapytanie HTTP, które zawiera ścieżkę do śledzonego pliku/folderu. 
- Usługa zaczyna wtedy raportować wszystkie zmiany do dziennika zdarzeń, do którego mamy podgląd z poziomu aplikacji. 
- Wysłanie kolejnego specjalnego zapytania powoduje, że usługa przestaje śledzić wybrany wcześniej folder.

## Funkcje śledzenia:

- Śledzenie folderu polega na raportowaniu dodania, usunięcia, zmiany nazwy oraz informacji o zmianie pliku.
- Śledzenie pliku polega na raportowaniu dodanych (+) lub usuniętych (-) linijek. Można śledzić tylko pliki .txt.

## Dodatkowe informacje:

- Biblioteka zawiera "logikę" watcherów, dzięki czemu jest używana zarówno w usłudze, jak i w aplikacji WPF.
- Pliki konfiguracyjne są używane do przechowywania nazwy oraz źródła dziennika zdarzeń, endpointów oraz nazwy usługi.
- Interfejs użytkownika jest przygotowany w taki sposób, że odpowiednie przyciski blokują się, gdy użytkownik działa, co minimalizuje błędy.

## Autor
Autor: Wojciech Duklas