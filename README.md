# Dance Studio Management Portal

[![Status: Active Development](https://img.shields.io/badge/Status-Active%20Development-blue)](https://github.com/ZeusDigitalDesigns/DanceStudioManagementPortal)
[![Framework: .NET 10 MVC](https://img.shields.io/badge/Framework-.NET%2010%20MVC-purple)](https://dotnet.microsoft.com/)
[![Language: C# 14](https://img.shields.io/badge/Language-C%23%2014-239120)](https://dotnet.microsoft.com/)
[![Database: SQL Server](https://img.shields.io/badge/Database-SQL%20Server-red)](https://www.microsoft.com/sql-server)

## Overview

The **Dance Studio Management Portal** is a full-stack web application architected to replace fragmented spreadsheets and manual administrative overhead with a centralized system for class registration, tuition billing, schedule coordination, and multi-tier account governance.

Built on **ASP.NET Core MVC (.NET 10)** with **Entity Framework Core 10** and **Microsoft SQL Server**, the platform enforces robust role-based access control (RBAC) across three primary operational tiers: **Parents/Dancers**, **Instructors**, and **Studio Directors**.

---

## Core Implemented Capabilities (Epics 1–6)

### 1. Identity, Authentication & RBAC (Epic 1)
* **Custom Identity Architecture:** Built on ASP.NET Core Identity with custom user entities and role profiles.
* **Account Lifecycle:** Secure registration, authentication, password recovery, and role assignment (`Parent`, `Instructor`, `Director`).
* **Profile Management:** Custom `Account/Manage` workflows for updating personal contact details, credentials, and notification preferences.

### 2. Parent & Dancer Registration Flows (Epic 2)
* **Household Setup:** Self-service registration flow for parents to establish studio profiles.
* **Student CRUD:** Complete dancer profile creation (names, dates of birth, skill levels, medical notes, emergency contacts).
* **Relationship Mapping:** Direct relational link between `ParentProfile` and associated `Dancer` records.

### 3. Financial Infrastructure & Parent Self-Service (Epic 3)
* **Live Itemized Ledgers:** Parent dashboard displaying real-time financial tracking for tuition charges, fees, and credits.
* **Payment Integration:** Secure checkout workflow leveraging Stripe.net SDK for processing one-off payments and installment balances.
* **Balance Tracking:** Automatic reconciliation of invoices against student accounts.

### 4. Instructor Class Operations (Epic 4)
* **Class Rostering:** Dedicated instructor views with active student enrollments and emergency contact flags.
* **Digital Attendance:** Interface for instructors to log daily student attendance and class statuses directly to the database.
* **Instructional Notes:** Class-level boards for choreography updates, announcements, and scheduling changes.

### 5. Director Administration & Governance (Epic 5)
* **Global Administrative Control:** Full CRUD control over studio-wide entities (classes, schedules, staff, and dancer rosters).
* **Billing Oversight:** Studio-level invoice creation, custom fee assignments, and manual ledger adjustments.
* **Analytics & Reporting:** Studio roster metrics, enrollment tracking, and preliminary revenue overviews.

### 6. Household Logic & Multi-Dancer Tuition Engine (Epic 6)
* **Multi-Dancer Grouping:** Relational bundling of sibling dancers under unified parent billing accounts.
* **Automated Sibling & Multi-Class Discounts:** Tiered tuition calculation engine that automatically applies studio discount policies prior to invoice generation.
* **Unified Family Invoicing:** Single, consolidated billing statements generated across all active students within a household.

---

## Tech Stack

* **Framework:** .NET 10 (ASP.NET Core MVC)
* **Language:** C# 14, Razor (HTML5 / CSS3 / JavaScript)
* **Data Access:** Entity Framework Core 10 (Code-First Migrations, Fluent API)
* **Database:** Microsoft SQL Server (Containerized / LocalDB / Azure SQL)
* **Authentication & Authorization:** ASP.NET Core Identity (Role-Based Access Control)
* **Third-Party Integrations:** Stripe.net SDK (Payments), Azure App Services (Deployment & CI/CD)

---

## Roadmap & Milestones

- [x] **Epic 1:** Public Website & Authentication (Identity, RBAC, Profile Management)
- [x] **Epic 2:** Parent/Dancer Registration & Profiles (Household Setup, Student CRUD)
- [x] **Epic 3:** Parent Dashboard & Financials (Live Ledgers, Stripe Checkout)
- [x] **Epic 4:** Instructor Portal & Class Operations (Rosters, Attendance)
- [x] **Epic 5:** Director Master Control (Global Management, Invoicing, Analytics)
- [x] **Epic 6:** Family & Multi-Dancer Logic (Household Grouping, Automated Discount Engine)
- [ ] **Epic 7:** Recital & Competition Logistics (Costume Tracking, Call Times, Video Vault)
- [ ] **Epic 8:** Digital Waivers & Legal Compliance (E-Signatures, Medical Flags)
- [ ] **Epic 9:** Fundraiser Credit Ledger (Balance Offsets & Account Crediting)
- [ ] **Epic 10:** Automated Communications (Twilio SMS / SendGrid Background Services)

---

## Getting Started

### Prerequisites
* [.NET 10 SDK](https://dotnet.microsoft.com/)
* [Visual Studio 2026](https://visualstudio.microsoft.com/) or [JetBrains Rider](https://www.jetbrains.com/rider/)
* Microsoft SQL Server (Docker Container, LocalDB, or SQL Server Express)
* Entity Framework Core CLI Tools:
  ```bash
  dotnet tool install --global dotnet-ef


## Author

### Zac Kimball
**Lead Developer & Software Architect** 

- Organization: Zeus Digital 
- DesignsStack: .NET 10 / C# 14 / SQL
- Server GitHub: @zkimball85

## License  Copyright © 2026 Zachary S. Kimball / Zeus Digital Designs. 

**All Rights Reserved.**   

Proprietary and Confidential. This repository and its contents are closed-source. Unauthorized copying, modification, distribution, or use of this software, via any medium, is strictly prohibite
