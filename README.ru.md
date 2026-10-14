[English](README.md)

---

<h1 align="center">
  <br>
  <a href="https://github.com/kameton-dev/SimpleDictaphone"><img width="130" height="130" alt="Ы" src="./SimpleDictaphone/Assets/images/logo-simpledictaphone.png" />
</a>
  <br>
  <b>SimpleDictaphone</b>
  <br>
</h1>

**SimpleDictaphone** - простое приложение для записи звука. Можно прослушивать аудиофайлы в приложении, и, при надобности, сохранять их в папку телефона

## Возможности

- Запись голоса в WAV <!-- TODO: уточнить формат и качество -->
- Список записей с группировкой по датам (сегодня / вчера / остальные)
- Воспроизведение: прогресс-бар, текущее и общее время, пауза, перемотка в начало
- Переименование и удаление записей
- Сохранение записи в `Music/SimpleDictaphone`, чтобы она появилась в музыке телефона

## Требования

- Устройство или эмулятор с Windows Phone 8.1
- Для сборки: Visual Studio 2013/2015 с Windows Phone 8.1 SDK

## Сборка и запуск

1. Клонируйте репозиторий:
   ```
   git clone https://github.com/kameton-dev/SimpleDictaphone.git
   ```
2. Откройте `SimpleDictaphone.sln` в Visual Studio.
3. Выберите **Emulator** или **Device** и нажмите **F5**.

<!-- ## Структура проекта

| Файл / папка | Назначение |
|---|---|
| `RecordingsPage.xaml(.cs)` | Список записей, воспроизведение, переименование, удаление, экспорт |
| `Resources/AppResources.resx` | Строки интерфейса (по одному `.resx` на язык) |
| `Assets/AppBar/` | Иконки панели приложения |

TODO: добавить страницу записи и остальные файлы -->

## Локализация

Все тексты интерфейса лежат в `Resources/AppResources*.resx`. Чтобы добавить язык, скопируйте `AppResources.resx` в `AppResources.<культура>.resx` (например, `AppResources.de-DE.resx`), переведите значения и добавьте культуру в поддерживаемые в настройках проекта.

## Хранение данных

Записи хранятся в изолированном хранилище приложения. Кнопка **Сохранить** копирует запись в `Music/SimpleDictaphone` на телефоне.

## Участие в разработке

Issues и pull request'ы приветствуются. Проект на раннем этапе, так что шероховатости возможны.

## Лицензия

Этот проект распространяется под лицензией [**GNU General Public License v3.0**](LICENSE).