# SqlIRR

I assume that if you have got here you are looking for a way to calculate IRR from a T-SQL script or stored procedure. I also assume you are familiar with SQL Server Database Projects.

## Setup and deployment

The project is a Visual Studio 2017 database project and is configured to deploy just the CLR and to create a function (dbo.SqlIRR).
Before you deploy you will need to do the following:

Sign the assembly in the project's Properties\SQLCLR\Signing dialog.

Enable CLR functions for your SQL Server (if not already enabled) to do this issue the following commands (needs sysadmin permissions)

```sql
sp_configure 'show advanced options', 1;  
GO  
RECONFIGURE;  
GO  
sp_configure 'clr enabled', 1;  
GO  
RECONFIGURE;  
GO 
```

## Usage

```sql
    DECLARE @Revenues AS NVARCHAR(MAX) = '-3000, 1850, 1400, 1000'
    SELECT dbo.SqlIRR(@Revenues)
```

## 

This project is heavily based on the work of Joseph A. Nyirenda and Mai Kalange. You can find more information about this at http://zainco.blogspot.com/2008/08/internal-rate-of-return-using-newton.html

Since creating this I have learned how to import the Microsoft Visual Basic Finance library into SQL Server CLR functions and leverage their capability
