# AMPS – Administrador de Matrícula, Promedio y Secuencial

AMPS es una aplicación móvil desarrollada como Proyecto Capstone para el curso de Ingeniería de Software II. Su propósito es ayudar a estudiantes universitarios a organizar su información académica desde un solo lugar, permitiendo manejar perfiles estudiantiles locales, secuencial académico, promedio/GPA y evidencias de matrícula.

La aplicación funciona de manera local en el dispositivo utilizando SQLite como base de datos. No requiere conexión a internet, login tradicional ni servidor externo.

---

## Integrantes del equipo

| Nombre | Número de estudiante |
|---|---|
| Eloim Borges | R00572231 |
| Brya Barbosa | R00581455 |

---

## Problema que resuelve

Muchos estudiantes universitarios manejan su información académica en diferentes lugares, como documentos PDF, fotos, archivos Word, cálculos manuales de promedio y listas separadas de cursos completados. Esto puede causar desorganización, pérdida de evidencia académica y dificultad para visualizar el progreso académico real.

AMPS centraliza esta información en una aplicación móvil sencilla, permitiendo que el estudiante pueda consultar, actualizar y organizar su información académica de forma clara.

---

## Objetivo del sistema

Desarrollar una aplicación móvil que permita administrar matrícula, promedio y secuencial académico mediante perfiles locales, almacenamiento SQLite y una interfaz simple para estudiantes universitarios.

---

## Público objetivo

AMPS está dirigido a estudiantes universitarios que desean llevar control de:

- cursos completados,
- cursos pendientes,
- progreso académico,
- promedio/GPA,
- evidencias de matrícula,
- documentos académicos importantes.

---

## Funcionalidades principales

### Gestión de perfiles

La aplicación permite crear y administrar múltiples perfiles estudiantiles locales dentro del mismo dispositivo. Cada perfil mantiene su propia información académica separada.

Funciones principales:

- crear perfil estudiantil,
- validar Student ID único,
- seleccionar perfil activo,
- editar nombre, nickname y correo electrónico,
- mantener datos separados por estudiante.

### Dashboard

El Dashboard es la pantalla principal del sistema. Muestra un resumen académico del perfil activo.

Incluye:

- nombre del estudiante,
- Student ID,
- correo electrónico,
- GPA actual,
- créditos completados,
- cursos detectados,
- cursos completados,
- progreso académico.

### Secuencial

El módulo de Secuencial permite administrar los cursos del estudiante.

Funciones principales:

- cargar documentos PDF o Word,
- extraer cursos automáticamente cuando sea posible,
- añadir cursos manualmente,
- editar cursos,
- eliminar cursos,
- marcar cursos como completados,
- guardar progreso académico.

### Promedio / GPA

El módulo de Promedio calcula el GPA del estudiante usando las notas registradas.

Funciones principales:

- guardar calificaciones,
- calcular GPA automáticamente,
- mostrar créditos completados,
- mostrar notas registradas,
- guardar historial reciente de cambios de GPA.

### Matrícula

El módulo de Matrícula permite guardar evidencia académica por semestre.

Funciones principales:

- crear matrícula por semestre,
- guardar evidencia en PDF,
- guardar evidencia en Word,
- guardar fotos,
- copiar archivos al almacenamiento interno de la app,
- abrir archivos guardados,
- eliminar matrículas con confirmación.

### Configuración

El módulo de Configuración permite ajustar preferencias y reiniciar datos del perfil activo.

Funciones principales:

- cambiar tema entre sistema, claro y oscuro,
- reiniciar Secuencial,
- reiniciar Promedio,
- reiniciar Matrícula.

Los reinicios solo afectan el perfil activo.

---

## Tecnologías utilizadas

| Tecnología | Uso |
|---|---|
| C# | Lógica principal de la aplicación |
| .NET MAUI | Desarrollo móvil multiplataforma |
| XAML | Diseño de interfaces |
| SQLite | Base de datos local |
| SQLite-net | Manejo de base de datos desde C# |
| Shell Navigation | Navegación entre pantallas |
| SecureStorage | Guardar perfil activo |
| Preferences | Guardar tema seleccionado |
| FilePicker | Selección de documentos y fotos |
| FileSystem | Almacenamiento interno de archivos |
| Launcher | Abrir archivos guardados |
| UglyToad.PdfPig | Lectura de archivos PDF |
| DocumentFormat.OpenXml | Lectura de archivos Word |
| GitHub | Control de versiones y colaboración |
| Visual Studio | Desarrollo y pruebas |

---

## Arquitectura general

AMPS utiliza una arquitectura móvil por capas.

```text
Presentation Layer
XAML Pages + Code-behind
Dashboard, Secuencial, Promedio, Matrícula, Perfiles, Settings

        ↓

Service Layer
DataBaseServices
ActiveProfileService
CourseExtractionService

        ↓

Data Layer
SQLite Local Database
Student, Course, Grade, GpaHistory, MatriculaItem, MatriculaFile

        ↓

Device Services
FilePicker, SecureStorage, Preferences, FileSystem, Launcher
```

---

## Base de datos

AMPS utiliza SQLite local. La información se organiza por perfil activo.

### Entidades principales

```text
Student
- Id
- StudentId
- Name
- Nickname
- Email

Course
- Id
- StudentDbId
- Codigo
- Nombre
- Creditos
- IsCompleted

Grade
- Id
- StudentDbId
- Materia
- Creditos
- Calificacion
- PuntosDeHonor

GpaHistory
- Id
- StudentDbId
- PreviousGpa
- NewGpa
- AddedCoursesSummary
- TotalCreditsAdded
- DateSaved

MatriculaItem
- Id
- StudentDbId
- Semestre
- Year
- FileMode
- DateSaved

MatriculaFile
- Id
- MatriculaItemId
- FileName
- StoredFileName
- FilePath
- FileType
- DateSaved
```

### Relaciones principales

```text
Student.Id 1 ─── * Course.StudentDbId
Student.Id 1 ─── * Grade.StudentDbId
Student.Id 1 ─── * GpaHistory.StudentDbId
Student.Id 1 ─── * MatriculaItem.StudentDbId

MatriculaItem.Id 1 ─── * MatriculaFile.MatriculaItemId
```

---

## Flujo funcional principal

```text
Primer inicio
   ↓
La app verifica si existen perfiles
   ↓
Si no existen perfiles, obliga a crear uno
   ↓
El perfil se guarda en SQLite
   ↓
Se establece como perfil activo
   ↓
La app navega al Dashboard
```

Luego el usuario puede acceder a:

```text
Dashboard
   ├── Secuencial
   ├── Promedio
   ├── Matrícula
   ├── Perfiles
   └── Configuración
```

En próximos inicios, la app restaura el perfil activo guardado usando SecureStorage.

---

## Estructura principal del proyecto

```text
AMPS/
│
├── App.xaml
├── App.xaml.cs
├── AppShell.xaml
├── AppShell.xaml.cs
├── MauiProgram.cs
│
├── DataBaseServices.cs
├── ActiveProfileService.cs
├── CourseExtractionService.cs
│
├── Student.cs
├── Course.cs
├── Grade.cs
├── GpaHistory.cs
├── MatriculaItem.cs
├── MatriculaFile.cs
├── ExtractedCourse.cs
│
├── Dashboard.xaml
├── Dashboard.xaml.cs
├── Secuencial.xaml
├── Secuencial.xaml.cs
├── Promedio.xaml
├── Promedio.xaml.cs
├── Matricula.xaml
├── Matricula.xaml.cs
├── ProfileCreation.xaml
├── ProfileCreation.xaml.cs
├── ProfileManagement.xaml
├── ProfileManagement.xaml.cs
├── SettingsPage.xaml
├── SettingsPage.xaml.cs
│
├── Platforms/
├── Resources/
└── Properties/
```

---

## Archivos principales y propósito

| Archivo | Propósito |
|---|---|
| `App.xaml.cs` | Carga la aplicación y aplica el tema guardado |
| `AppShell.xaml.cs` | Registra rutas y controla navegación inicial |
| `MauiProgram.cs` | Configura servicios, páginas y base de datos |
| `DataBaseServices.cs` | Maneja SQLite y operaciones de datos |
| `ActiveProfileService.cs` | Mantiene el perfil activo |
| `CourseExtractionService.cs` | Extrae cursos desde PDF o Word |
| `Dashboard.xaml.cs` | Carga resumen académico |
| `Secuencial.xaml.cs` | Maneja cursos y progreso académico |
| `Promedio.xaml.cs` | Calcula GPA y muestra notas |
| `Matricula.xaml.cs` | Guarda evidencias académicas |
| `ProfileCreation.xaml.cs` | Crea perfiles estudiantiles |
| `ProfileManagement.xaml.cs` | Administra perfiles |
| `SettingsPage.xaml.cs` | Maneja tema y reinicios |

---

## Instalación y ejecución

### Requisitos

- Visual Studio 2022 o superior.
- Workload de .NET MAUI instalado.
- .NET 9.
- Emulador Android, dispositivo físico o Windows target.
- Git instalado.

### Pasos para ejecutar

1. Clonar el repositorio:

```bash
git clone <repository-url>
```

2. Abrir la solución:

```text
AMPS.sln
```

3. Restaurar paquetes NuGet si Visual Studio no lo hace automáticamente.

4. Seleccionar plataforma de ejecución:

```text
Android Emulator
Windows Machine
iOS Simulator
Mac Catalyst
```

5. Ejecutar el proyecto desde Visual Studio.

---

## Dependencias principales

El proyecto utiliza paquetes para SQLite local, lectura de PDF, lectura de Word y servicios de .NET MAUI.

Librerías destacadas:

```text
sqlite-net-pcl
UglyToad.PdfPig
DocumentFormat.OpenXml
```

---

## Evidencia de SCRUM

El proyecto fue organizado utilizando prácticas Agile/SCRUM.

Se trabajó con:

- Product Backlog,
- Sprint Backlog,
- Weekly Sprint Topics,
- alineación entre backlog y sprints,
- pruebas funcionales,
- pruebas de UI,
- pruebas unitarias,
- entregas progresivas.

### Sprints principales

| Semana | Tema |
|---|---|
| Semana 1 | Database Schemes and Mainframe |
| Semana 2 | User Interface |
| Semana 3 | Text Analyzer |
| Semana 4 | Profile Management |
| Semana 5 | Dashboard and Navigation |
| Semana 6 | Course Tracking System |
| Semana 7 | GPA and Grade Management |
| Semana 8 | Enrollment Record Storage |
| Semana 9 | Testing and Final Adjustments |

---

## Estado actual del proyecto

AMPS se encuentra en etapa funcional/final. Los módulos principales están implementados y conectados mediante perfil activo.

Módulos completados:

- perfiles,
- dashboard,
- secuencial,
- promedio/GPA,
- matrícula,
- configuración,
- almacenamiento local,
- extracción básica desde documentos,
- pruebas y documentación.

---

## Mejoras futuras

Algunas mejoras que podrían implementarse en versiones futuras son:

- respaldo en la nube,
- exportación/importación de base de datos,
- soporte para más formatos de documentos,
- mejoras visuales en la interfaz,
- estadísticas académicas más avanzadas,
- sincronización entre dispositivos,
- soporte multilenguaje,
- sistema de notificaciones.

---
