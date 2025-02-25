# SmallBusinessLendingDesktop
## Overview

The **SmallBusinessLendingDesktop** is an open-source Windows desktop application that helps users validate **Small Business Lending Data** in accordance with the **CFPB (Consumer Financial Protection Bureau) Filing Instructions Guide (FIG)**. 

This tool allows users to:
- Import a **Small Business Lending CSV file**.
- Set the **Legal Entity Identifier (LEI)**.
- Define the **reporting start date** and **end date**.
- Validate the file against CFPB compliance rules.
- Generate a detailed **error report** for corrections.

## Features

✅ **Import CSV File** – Load small business lending data for validation.  
✅ **Set LEI** – Enter and save your **Legal Entity Identifier**.  
✅ **Set Reporting Period** – Define **start date** and **end date** for validation.  
✅ **Comprehensive Validation** – Checks all rules from the **CFPB Small Business Lending FIG**.  
✅ **Error Report Generation** – Displays errors in an easy-to-understand format.  
✅ **User-Friendly Interface** – Simple and intuitive Windows Forms UI.  

## Installation

### Prerequisites
- **Windows 10/11**  
- **.NET Framework 4.8+** (or .NET 6+ if applicable)  
- **Visual Studio** (for development)

### Steps to Run the Application
1. **Clone the Repository**  
   ```sh
   git clone https://github.com/mohdrafi17/SmallBusinessLendingDesktop.git
   cd SmallBusinessLendingDesktop
2. Open in Visual Studio
   Open SmallBusinessLending.sln in Visual Studio.

3. Build and Run

    Click Start (F5) to compile and run the application.

   Usage
Launch the Application.
Click "Import File" and select your CSV file.
Enter LEI in the provided input field.
Select Reporting Start Date and End Date using the date pickers.
Click "Validate" to check for errors.
Review Validation Errors in the displayed list.
Save the error report for correction.
Validation Rules
The tool checks for compliance with the CFPB Small Business Lending Filing Instructions Guide (FIG), including:

Date Validations: Ensuring the Action Taken Date falls within the reporting period.
Field-Level Validations: Checking required fields, numeric formats, and text constraints.
Conditional Rules: Ensuring correct relationships between fields, such as num_principal_owners vs. demographic data.
Contributing
We welcome contributions from the community! To contribute:


Create a feature branch (git checkout -b feature-name).
License
This project is licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE for details.

Contact
For questions, issues, or contributions, open a GitHub Issue or reach out via:
📧 Email: mohdrafi17@gmail.com
🌐 GitHub: github.com/mohdrafi17
