# 📋 Инструкция по настройке UI

## 🎨 Шаг 1: Создание сцены главного меню

### 1.1. Создайте новую сцену
```
File → New Scene → Empty Scene
Сохраните как: Assets/Scenes/MainMenu.unity
```

### 1.2. Добавьте Canvas
```
GameObject → UI → Canvas
```

**Настройки Canvas:**
- **Canvas Scaler**:
  - UI Scale Mode: `Scale With Screen Size`
  - Reference Resolution: `1920 x 1080`
  - Match: `0.5`

### 1.3. Создайте панель меню
```
Правой кнопкой на Canvas → UI → Panel
Назовите: "MainMenuPanel"
```

**Настройки RectTransform:**
- Pos X: `0`, Pos Y: `0`
- Width: `400`, Height: `500`
- Anchor: `Center`

**Настройки Panel:**
- Color: `RGBA(30, 30, 30, 255)` (тёмно-серый)

### 1.4. Добавьте заголовок
```
Правой кнопкой на MainMenuPanel → UI → Text - TextMeshPro
Назовите: "TitleText"
```

**Настройки:**
- Text: `DIPLOMA PROJECT`
- Font Size: `48`
- Color: `White`
- RectTransform:
  - Pos Y: `200`
  - Width: `350`, Height: `60`

### 1.5. Добавьте поле ввода Seed
```
Правой кнопкой на MainMenuPanel → UI → Input Field - TextMeshPro
Назовите: "SeedInputField"
```

**Настройки:**
- RectTransform:
  - Pos Y: `100`
  - Width: `200`, Height: `40`
- **TMP Input Field** (не InputField!):
  - Placeholder: `Enter Seed (e.g. 12345)`
  - Content Type: `Integer Number`
  - Caret Blink Rate: `0.85`

### 1.6. Добавьте кнопку рандомизации (слева от поля)
```
Правой кнопкой на MainMenuPanel → UI → Button - TextMeshPro
Назовите: "RandomizeButton"
```

**Настройки RectTransform:**
- Pos X: `-160` (слева от поля)
- Pos Y: `100`
- Width: `40`, Height: `40`

**Текст кнопки:**
- Text: `🎲` (или "RND")
- Font Size: `20`

### 1.7. Добавьте кнопку старта
```
Правой кнопкой на MainMenuPanel → UI → Button - TextMeshPro
Назовите: "StartButton"
```

**Настройки:**
- RectTransform:
  - Pos Y: `20`
  - Width: `200`, Height: `50`
- Button Text:
  - Text: `START GAME`
  - Font Size: `24`
  - Color: `Black`
- Button Colors:
  - Normal: `RGBA(76, 175, 80, 255)` (зелёный)
  - Highlighted: `RGBA(56, 142, 60, 255)`

### 1.8. Добавьте кнопку выхода
```
Правой кнопкой на MainMenuPanel → UI → Button - TextMeshPro
Назовите: "ExitButton"
```

**Настройки:**
- RectTransform:
  - Pos Y: `-40`
  - Width: `200`, Height: `50`
- Button Text:
  - Text: `EXIT`
  - Font Size: `24`
- Button Colors:
  - Normal: `RGBA(244, 67, 54, 255)` (красный)
  - Highlighted: `RGBA(198, 40, 40, 255)`

---

## 🎮 Шаг 2: Создание игровой сцены с паузой

### 2.1. Откройте сцену Main
```
Assets/Scenes/Main.unity
```

### 2.2. Добавьте Canvas для паузы
```
GameObject → UI → Canvas
Назовите: "PauseCanvas"
```

**Настройки Canvas:**
- Render Mode: `Screen Space - Overlay`
- Canvas Scaler: как в MainMenu

### 2.3. Создайте панель паузы
```
Правой кнопкой на PauseCanvas → UI → Panel
Назовите: "PauseMenuPanel"
```

**Настройки RectTransform:**
- Anchor: `Center`
- Pos X: `0`, Pos Y: `0`
- Width: `300`, Height: `250`

**Настройки Panel:**
- Color: `RGBA(0, 0, 0, 200)` (полупрозрачный чёрный)

### 2.4. Добавьте заголовок паузы
```
Правой кнопкой на PauseMenuPanel → UI → Text - TextMeshPro
Назовите: "PauseTitle"
```

**Настройки:**
- Text: `PAUSED`
- Font Size: `36`
- Color: `White`
- RectTransform:
  - Pos Y: `80`
  - Width: `250`, Height: `40`

### 2.5. Добавьте кнопку "В главное меню"
```
Правой кнопкой на PauseMenuPanel → UI → Button - TextMeshPro
Назовите: "MainMenuButton"
```

**Настройки:**
- RectTransform:
  - Pos Y: `10`
  - Width: `200`, Height: `40`
- Button Text:
  - Text: `Main Menu`
  - Font Size: `20`

### 2.6. Добавьте кнопку "Выход"
```
Правой кнопкой на PauseMenuPanel → UI → Button - TextMeshPro
Назовите: "ExitButton"
```

**Настройки:**
- RectTransform:
  - Pos Y: `-40`
  - Width: `200`, Height: `40`
- Button Text:
  - Text: `Exit Game`
  - Font Size: `20`

---

## 🔧 Шаг 3: Настройка скриптов

### 3.1. На MainMenu сцене:

1. **Создайте пустой GameObject:**
   ```
   GameObject → Create Empty
   Назовите: "UI_Managers"
   ```

2. **Добавьте компонент MainMenuManager:**
   ```
   Add Component → MainMenuManager
   ```

3. **Назначьте ссылки в инспекторе:**
   - **Seed Input Field**: перетащите `SeedInputField`
   - **Randomize Button**: перетащите `RandomizeButton`
   - **Start Button**: перетащите `StartButton`
   - **Exit Button**: перетащите `ExitButton`
   - **Game Scene Name**: `Main` (или название вашей игровой сцены)

### 3.2. На игровой сцене Main:

1. **Создайте пустой GameObject:**
   ```
   GameObject → Create Empty
   Назовите: "UI_Managers"
   ```

2. **Добавьте компонент PauseMenuManager:**
   ```
   Add Component → PauseMenuManager
   ```

3. **Назначьте ссылки в инспекторе:**
   - **Pause Menu Panel**: перетащите `PauseMenuPanel`
   - **Main Menu Button**: перетащите `MainMenuButton`
   - **Exit Button**: перетащите `ExitButton`
   - **Menu Scene Name**: `MainMenu`
   - **Pause Key**: `Escape` (по умолчанию)

4. **На WorldBootstrap:**
   - Включите галочку **Use Player Prefs Seed**

---

## 🎯 Шаг 4: Настройка Build Settings

```
File → Build Settings
```

**Добавьте сцены в порядке:**
1. `MainMenu` (index 0)
2. `Main` (index 1)

---

## ✅ Проверка работы

### 4.1. Запустите сцену MainMenu:
- Поле Seed должно показывать `12345`
- Кнопка 🎲 должна генерировать случайный seed
- Кнопка START должна загружать сцену Main
- Кнопка EXIT должна закрывать игру

### 4.2. В сцене Main:
- Нажмите ESC — должно появиться меню паузы
- Кнопка "Main Menu" должна возвращать в меню
- Кнопка "Exit" должна закрывать игру

### 4.3. Проверка seed:
1. Введите seed в главном меню (например, `99999`)
2. Нажмите START
3. В консоли должно быть: `[WorldBootstrap] Using seed from PlayerPrefs: 99999`
4. Нажмите ESC → Main Menu
5. Введите другой seed (например, `77777`)
6. Нажмите START
7. В консоли должно быть: `[WorldBootstrap] Using seed from PlayerPrefs: 77777`

---

## 🎨 Дополнительные улучшения (опционально)

### Анимации UI:
```csharp
// Добавьте в MainMenuManager
using UnityEngine.EventSystems;

public void OnPointerEnter() {
    // Анимация при наведении
}
```

### Звуки:
```csharp
// Добавьте AudioSource на UI_Managers
public AudioClip buttonClickSound;

private void PlayClickSound() {
    AudioSource.PlayClipAtPoint(buttonClickSound, Camera.main.transform.position);
}
```

### Сохранение последнего seed:
```csharp
// В MainMenuManager.Start()
if (PlayerPrefs.HasKey("LastSeed")) {
    seedInputField.text = PlayerPrefs.GetString("LastSeed");
}
```

---

## 📸 Скриншоты для проверки

После настройки сделайте скриншоты:
1. Главное меню с полем Seed
2. Меню паузы с кнопками
3. Консоль с сообщением о seed

Это понадобится для диплома!
