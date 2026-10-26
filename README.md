\# Staybnb 🏡



\*\*A web-based accommodation booking platform built with ASP.NET Core MVC and C#.\*\*



Staybnb is a web application designed to connect guests with property hosts. Users can explore available properties, manage bookings, communicate through the platform, and apply to become hosts. Administrators can review host applications and manage platform activity.



The project was developed as a practical demonstration of web application development, database management, authentication, authorisation, and CRUD operations.



\## 📌 Features



\### 👤 User Accounts



\* User registration and login.

\* Secure authentication using ASP.NET Core Identity.

\* User roles and role-based access control.

\* Role-specific navigation and dashboards.



\### 🏠 Property Listings



\* Browse available accommodation.

\* Filter properties by city and property type.

\* View property details, descriptions, images, and nightly prices.

\* Hosts can manage their own property listings.



\### 📅 Booking Management



\* Create accommodation bookings.

\* Calculate booking costs based on nightly rates, duration, and applicable fees.

\* Prevent conflicting bookings for the same property.

\* View booking details and booking statuses.

\* Hosts can approve or reject booking requests.



\### 🤝 Host Applications



\* Guests can apply to become hosts.

\* Submit property details, descriptions, pricing, and images.

\* Administrators can review applications and approve or reject them.

\* Approved applications create property listings and update the applicant's role.



\### 🔑 Guest Check-In



\* Access check-in instructions for approved bookings.

\* Upload guest documents for the check-in process.

\* Hosts can access and verify documents associated with their bookings.



\### 💬 Messaging and Notifications



\* Send and receive messages through the application.

\* View inbox and sent messages.

\* Receive notifications about relevant application events.

\* Manage notification read status.



\### 🛡️ Administration



\* Review host applications.

\* Manage application approval decisions.

\* View administrative activity logs.

\* Restrict administrative functionality according to user roles.



\### 🎨 User Interface



\* Responsive layouts for desktop and mobile screens.

\* Consistent salmon-pink, white, and charcoal colour scheme.

\* Dedicated pages for browsing properties, bookings, dashboards, messaging, and account management.



\## 🛠️ Technologies Used



| Technology            | Purpose                                 |

| --------------------- | --------------------------------------- |

| C#                    | Application programming language        |

| ASP.NET Core MVC      | Web application framework               |

| Entity Framework Core | Database access and ORM                 |

| Microsoft SQL Server  | Relational database                     |

| ASP.NET Core Identity | Authentication and user management      |

| Razor Views           | Dynamic HTML rendering                  |

| HTML5                 | Page structure                          |

| CSS3                  | Styling and responsive layouts          |

| Bootstrap             | Layout components and responsive design |

| JavaScript            | Client-side interactions and validation |

| Git and GitHub        | Version control and source code hosting |



\## 🏗️ Application Architecture



Staybnb follows the Model-View-Controller (MVC) architectural pattern.



\* \*\*Models:\*\* Represent application entities, including properties, bookings, messages, notifications, and host applications.

\* \*\*Views:\*\* Razor pages provide the user interface.

\* \*\*Controllers:\*\* Handle incoming requests, application logic, validation, and interactions between models and views.

\* \*\*Database:\*\* SQL Server stores application data, while Entity Framework Core manages database interactions.

\* \*\*Identity:\*\* Manages user accounts, authentication, and roles.



\## 👥 User Roles



The application uses role-based authorisation to separate access to different features.



| Role       | Main Responsibilities                                                                          |

| ---------- | ---------------------------------------------------------------------------------------------- |

| Guest      | Browse properties, manage bookings, send messages, and apply to become a host                  |

| Host       | Manage properties, review bookings, configure check-in information, and verify guest documents |

| Admin      | Review host applications and access administrative activity logs                               |

| SuperAdmin | Reserved for elevated administrative permissions, including promoting users to Admin           |



Access to restricted actions is controlled through authentication, role checks, and ownership validation where applicable.



\## ⚙️ Prerequisites



To run Staybnb locally, you will need:



\* Windows or another supported development environment.

\* A compatible .NET SDK matching the project's target framework.

\* Visual Studio or Visual Studio Code.

\* Microsoft SQL Server.

\* Git (optional, but recommended).



\## 🚀 Getting Started



\### 1. Clone the repository



Open a terminal and run:



```bash

git clone <YOUR\_GITHUB\_REPOSITORY\_URL>

```



Navigate to the cloned project directory:



```bash

cd Staybnb

```



Replace `<YOUR\_GITHUB\_REPOSITORY\_URL>` with your actual GitHub repository URL.



\### 2. Restore dependencies



Run:



```bash

dotnet restore

```



\### 3. Configure the database connection



Open `appsettings.json` and check the database connection string.



The development configuration may use a local SQL Server instance:



```json

{

&#x20; "ConnectionStrings": {

&#x20;   "DefaultConnection": "Server=localhost;Database=StaybnbDb;Trusted\_Connection=True;TrustServerCertificate=True;"

&#x20; }

}

```



Adjust the connection string to match your local SQL Server installation.



\*\*Security note:\*\* Do not commit database passwords, production credentials, or other secrets to GitHub.



\### 4. Apply database migrations



If the project already contains Entity Framework Core migrations, run:



```bash

dotnet ef database update

```



If the Entity Framework command-line tool is not installed, consult the official Entity Framework Core documentation for installation instructions.



If migrations have not yet been created, create and review the initial migration before applying it:



```bash

dotnet ef migrations add InitialCreate

dotnet ef database update

```



Do not create another initial migration if the project already has the required migration history.



\### 5. Run the application



Start the application using:



```bash

dotnet run

```



Open the local URL displayed in the terminal in your browser.



\## 🔐 Security Considerations



Staybnb incorporates several application security measures:



\* Authentication through ASP.NET Core Identity.

\* Role-based authorisation for restricted functionality.

\* Ownership checks for protected property, booking, and document operations.

\* Server-side validation of submitted information.

\* File type and file size restrictions for supported uploads.

\* Storage of guest documents outside the publicly accessible `wwwroot` directory.

\* Antiforgery protection for applicable form submissions.



For production deployment, additional review is required for database credentials, HTTPS, file upload handling, data retention, logging, and privacy compliance.



\## 📁 Project Structure



The project follows the standard ASP.NET Core MVC structure.



```text

Staybnb/

├── Controllers/

├── Data/

├── Models/

├── ViewModels/

├── Views/

│   ├── Account/

│   ├── Admin/

│   ├── Bookings/

│   ├── Host/

│   ├── HostApplications/

│   ├── Messages/

│   ├── Notifications/

│   ├── Properties/

│   └── Shared/

├── wwwroot/

│   ├── css/

│   ├── js/

│   └── uploads/

├── App\_Data/

├── appsettings.json

├── Program.cs

├── Staybnb.csproj

├── .gitignore

└── README.md

```



\*This structure is representative. Some folders may be created or populated as the application runs or as development continues.\*



\## 🧪 Testing



Before using the application, test the main workflows:



\* Register, log in, and log out.

\* Browse and filter property listings.

\* Submit a host application with property images.

\* Approve or reject a host application as an administrator.

\* Create bookings and verify booking cost calculations.

\* Confirm that conflicting bookings are rejected.

\* Verify role and ownership restrictions.

\* Test messaging and notifications.

\* Test guest document uploads and host verification.

\* Confirm that restricted pages cannot be accessed by unauthorised users.



\## 📚 Project Purpose



Staybnb demonstrates the application of web development concepts, including MVC architecture, object-relational mapping, relational database design, authentication, authorisation, CRUD operations, file uploads, and responsive user interface development.



The project also provides practical experience in integrating multiple application features into a single web-based system.



\## 👨‍💻 Author



Developed as an academic software development project.



\## 📄 License



This repository is provided for educational and demonstration purposes. No specific open-source licence is granted unless a licence file is added to the repository.



