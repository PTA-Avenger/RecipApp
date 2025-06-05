# 🍲 RecipeApp

![CI](https://github.com/PTA-Avenger/RecipApp/actions/workflows/build-apk.yml/badge.svg)
[![Download APK](https://img.shields.io/badge/Download-APK-blue.svg)](https://github.com/PTA-Avenger/RecipApp/releases/latest/download/RecipeApp.apk)
![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)

## Table of Contents
- [Features](#-features)
- [Screenshots](#-screenshots)
- [Getting Started](#-getting-started)
- [Project Structure](#-project-structure)
- [Technologies Used](#-technologies-used)
- [Contributing](#-contributing)
- [FAQ](#-faq)
- [License](#-license)
- [Author](#-author)

---

## 📱 Features

- 📖 Browse a curated list of recipes
- 🔍 Search by name, ingredients, or category
- 📝 Add and manage personal recipes
- 🌗 Light/Dark theme support
- 🌍 Cross-platform (Android, Windows, macOS, iOS*)
- 📦 Built with .NET MAUI and C#
- 📲 Android APK available for download

---

## 🚀 Getting Started

### Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)
- [.NET MAUI Workloads](https://learn.microsoft.com/en-us/dotnet/maui/get-started/installation)
- Visual Studio 2022+ (with Mobile Development workload)

### Clone the Repository

```bash
git clone https://github.com/PTA-Avenger/RecipApp.git
cd RecipApp
```

### Build and Run (Android Example)

```bash
dotnet workload install maui-android
dotnet restore
dotnet build -f net9.0-android
dotnet run -f net9.0-android
```

---

## 🛠️ Continuous Integration (CI)

This project includes a CI/CD GitHub Actions workflow that:
- Restores dependencies
- Installs MAUI Android workload
- Builds the Android .apk
- Publishes it as an artifact for download

📦 The latest .apk is available under the [Actions](https://github.com/PTA-Avenger/RecipApp/actions) tab after each push to main.

### 📤 Download APK

You can download the latest Android APK by:
1. Visiting the [Actions tab](https://github.com/PTA-Avenger/RecipApp/actions)
2. Selecting the latest successful build on main
3. Scrolling to the Artifacts section and downloading `RecipeApp-APK`

*Coming Soon: Automatic GitHub Release with direct public APK download*

---

## 🧱 Project Structure

```
RecipeApp/
├── Platforms/           # Platform-specific startup code
├── Pages/               # UI pages and views
├── Models/              # Data models (e.g., Recipe, User)
├── ViewModels/          # MVVM logic and bindings
├── Resources/           # Images, styles, fonts
├── Helpers/             # Utility/helper files
├── RecipeApp.csproj     # MAUI project file
└── README.md
```

---

## 📄 Technologies Used

- .NET MAUI
- C#
- GitHub Actions (CI/CD)
- MVVM Architecture
- XAML UI

---

## 📈 Contributing

Contributions are welcome! To contribute:

1. Fork the repository
2. Create a new branch (`git checkout -b feature-name`)
3. Commit your changes following the existing code style
4. Run tests if applicable
5. Push to your fork and submit a Pull Request

Please ensure your code adheres to the project's coding standards.

---

## ❓ FAQ

**Q: Why is the APK only available for Android?**  
A: .NET MAUI supports multiple platforms, but current CI/CD automation focuses on Android. Support for other platforms may be added in the future.

**Q: What is .NET MAUI?**  
A: [.NET MAUI](https://learn.microsoft.com/en-us/dotnet/maui/what-is-maui) is a cross-platform framework for creating native mobile and desktop apps with C# and XAML.

---

## 📜 License

This project is licensed under the [MIT License](LICENSE).

---

## 👤 Author

PTA-Avenger  
[GitHub](https://github.com/PTA-Avenger)

---

**How to update this README:**
- Replace screenshot links with actual images.
- Update FAQ as new questions arise.
- Add more badges if desired (code coverage, issues, etc.).
