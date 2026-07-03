HealthSphere - Clinic Management System

This is a complete Clinic Management System built using ASP.NET MVC. It includes modules for appointment booking, doctor management, user registration, login, and email notifications.

🔐 Default Login Credentials

Admin Panel
Email: admin@gmail.com  
Password: 12341234

Pharmacist Panel
Email: pharmacist@gmail.com  
Password: 12341234

Doctor Panel
Email: doctor@gmail.com  
Password: 12341234

Receptionist Panel
Email: receptionist@gmail.com  
Password: 12341234

User  Panel
Email: user@gmail.com  
Password: 12341234

Patient Panel
Email: patient@gmail.com  
Password: 12341234

---

📧 Email Configuration

To enable email sending features (like appointment confirmation, register, etc.), update your SMTP settings in the `appsettings.json` file:

json
"EmailSettings": {
  "Host": "smtp.yourmailprovider.com",
  "Port": 587,
  "UserName": "youremail@example.com",
  "Password": "your_email_password",
  "CCEmail": "youremail@example.com"
}

---

The database backup file is included in the project inside the /Database folder. The file is named:
/Database/HealthSphere.bacpac

👉 How to Import .bacpac in SQL Server (SSMS):
Open SQL Server Management Studio (SSMS).

Connect to your local SQL Server instance.

In Object Explorer, right-click on Databases and select:

Import Data-tier Application...

Click Next, then select:

Import from Local Disk

Browse to the path:

/Database/HealthSphere.bacpac

Click Next, then specify a new Database Name (e.g., HealthSphere).

Click Next again, then click Finish.

Wait for the import process to complete.

Once done, you will see the HealthSphere database listed under your Databases in SSMS.

⚠️ Important:
Make sure your SQL Server is running and has enough permissions to create a new database.

If you’re using SQL Server Express, ensure it supports importing .bacpac files (or use SQL Server Developer edition).

After importing, update the connection string in your appsettings.json file accordingly.

"ConnectionStrings": {
  "dbcs": "Server=YOUR_SERVER_NAME;Database=HealthSphere;Trusted_Connection=True;TrustServerCertificate=True;"
}

Replace YOUR_SERVER_NAME with your actual SQL Server instance name (like localhost\\SQLEXPRESS).
