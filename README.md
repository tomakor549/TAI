# Aplikacja do przesyłania i pobierania plików

## Spis treści
* Informacje ogólne
* Technologia
* Opis kodu
* Uruchomienie

## Informacje ogólne
Aplikacja, która umożliwia przesyłanie z lokalnego dysku i pobranie do lokalnego dysku plików o dowolnym rozszerzeniu. 
Przesłać i pobrać plik można z wykorzystaniem: przesyłu tradycyjnego, tradycyjnego szyfrowanego oraz strumieniowo, i poznać 
ilość czasu, którego potrzebował algorytm aby wykonać operację.

## Technologia
Program został napisany w języku c# w platformie MVC w ASP.NET Core ver. 6.0.

## Kod
Głownymi plikami kodu są:
* Controllers
* Data
* Models
* SpecialOperation
* Users
* Views

### Controllers
W pliku tym są następujące pliki:
#### FileOperationController.cs 
Są zaimplementowane wszystkie operacje przesyłu i pobrania plików.

#### HomeController.cs
Jest zaimpelmentowane utworzenie nowego użytkownika, oraz listę zarejestrowanych użytkowaników.

### Data
W pliku tym są następujące są kody generowane automatycznie podczas uruchomienia programu.
Są to pliki dotyczące bazy danych zarejestrowanych użytkowników (wraz z informacją o tym, czy są zalogowani bądź nie).

### Models
W pliku tym są następujące pliki:
#### ApplicationUser.cs
Odpowiada za określenie lokalizacji użytkownika (zalogowany czy nie)

#### ErrorViewModel.cs
Sprawdza czy profil użytkownika jest pusty.

#### HomeModel.cs
Lista nazw przesłanych plików.

### SpecialOperation
W pliku tym są następujące pliki:
#### FileEncrypytionOperations.cs
Klasy odpowiedzialne za zaszyfrowanie plików AES256, zarówno pobieranych jak i przesyłanych.

#### MyFileStream.cs
Odpowiada za czas od kiedy i do kiedy ma zostać odliczany czas przesyłu/pobrania pliku.

### Users
Zawiera pliki odpowiadające wszystkim zarejestrowanym użytkownikom w których są wszystkie przesłane przez nich pliki.

##Views
### Operations.cshtml
Kod odpwiadający za wyświetlenie strony z możliwością przesyłu plików.

### Index.cshtml
Kod odpwiadający za wyświetlenie strony z możliwością pobrania plików.

## Urochomienie
W terminalu wpisać komendę:
```
 Update-Database
```
Uruchomić projekt.

Wciskając 'Register' można utworzyć nowego użytkownika, a 'Login' zalogować się.
Po zalogowaniu można wejść w zakładkę 'Files Operations', gdzie można przesłać plik wybraną metodą.
Można też wejść w zakładkę Home, gdzie jestlista przesłanych plików, możnaa je również pobrać na dysk lokalny.
