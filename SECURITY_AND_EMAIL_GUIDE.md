# Intallation packeages
1. 


# Employee Management System - Email + Security Guide

## PART 1: Setup (sirf ek baar)

### A) Gmail App Password banao
Gmail ka normal password code mein nahi chalta. "App Password" banana padta hai.
1. Gmail account mein **2-Step Verification ON** karo (myaccount.google.com > Security).
2. https://myaccount.google.com/apppasswords kholo.
3. App ka naam likho (jaise "EmployeeApp") aur **Create** dabao.
4. 16 letters ka password milega. Use copy kar lo.

### B) User Secrets mein save karo (project folder mein terminal kholo)
```
dotnet user-secrets set "EmailSettings:SenderEmail" "yourgmail@gmail.com"
dotnet user-secrets set "EmailSettings:AppPassword" "abcdefghijklmnop"
dotnet user-secrets set "SeedAdmin:Password" "YourStrong@Pass1"
```
- Admin password kam se kam 8 characters: uppercase + lowercase + number + symbol.
- Agar admin pehle se bana hua hai, to naya password set karne se purana nahi badlega.

### C) Run
```
dotnet run
```
Test: employee ka email asli Gmail rakho, leave apply karo, Admin se Approve/Reject karo.
Email "Leave Request Approved/Rejected" subject ke saath employee ke naam se jayegi.

---

## PART 2: Email kaise kaam karti hai (explain karne ke liye)
1. Admin **Approve/Reject** dabata hai -> `LeavesController.Decide` chalta hai.
2. Pehle status database mein save hota hai.
3. Phir `EmailService` Gmail SMTP (smtp.gmail.com, port 587, TLS) se mail bhejti hai.
4. Mail ka text ready template hai: "Dear <Employee Name>, your <type> leave from <date> to <date> has been Approved/Rejected" + HR remark + respectful closing.
5. Agar mail fail ho jaye to app crash nahi hota; Admin ko warning dikhti hai.

---

## PART 3: Security - kya lagaya aur kyun (simple explanation)

| # | Security | Kya karta hai | Kahan hai |
|---|----------|---------------|-----------|
| 1 | **Password hashing** | Password database mein plain text nahi, hash hota hai. DB leak hone par bhi asli password nahi milta. | ASP.NET Identity |
| 2 | **Strong password rules** | Min 8 chars + upper + lower + number + symbol. | Program.cs |
| 3 | **Account lockout** | 5 galat try ke baad 15 min ke liye account lock (brute force attack rokta hai). | Program.cs |
| 4 | **Role based access** | Admin pages sirf Admin ko, User pages sirf Employee ko. | `[Authorize(Roles=...)]` |
| 5 | **Secure by default** | Jis page par login zaroori nahi likha, wahan bhi login zaroori. Naya page galti se open nahi rehta. | FallbackPolicy |
| 6 | **CSRF protection** | Har form mein secret token; dusri website aapke naam se form submit nahi kar sakti. | AutoValidateAntiforgeryToken |
| 7 | **SQL Injection se bachav** | Entity Framework parameterized queries use karta hai; user ka text SQL command nahi ban sakta. | EF Core |
| 8 | **XSS se bachav** | Razor page par text apne aap encode karta hai; `<script>` likhne par wo chalta nahi. Email mein bhi HtmlEncode. | Razor + EmailService |
| 9 | **Safe cookie** | HttpOnly (JS padh nahi sakti), SameSite Strict, Secure (live par HTTPS), 30 min baad auto logout. | Program.cs |
| 10 | **HTTPS + HSTS** | Data encrypted jata hai. | Program.cs |
| 11 | **Security headers** | Clickjacking (X-Frame-Options), MIME sniffing (nosniff) rokte hain. | Program.cs |
| 12 | **Secrets code mein nahi** | Gmail App Password aur Admin password User Secrets mein; code/zip share karne par leak nahi hote. | User Secrets |
| 13 | **Error hide** | Live mode mein hacker ko stack trace nahi dikhti, sirf simple error page. | /Account/Error |
| 14 | **Input validation** | Server side + client side: required, email, 10 digit phone, salary range, unique code/email. | Models |
| 15 | **Business rules** | Leave sirf ek baar decide ho sakti hai; purani date aur overlapping leave nahi; register sirf Admin ke add kiye employee ka. | Controllers |
| 16 | **Generic error messages** | Register/Login par kaun sa email exist karta hai, ye nahi batate (user enumeration rokta hai). | AccountController |

### Honest note (viva/presentation mein bol sakte ho)
"Koi bhi system 100% unhackable nahi hota. Humne OWASP ke common attacks (SQL Injection, XSS, CSRF, Brute Force, Session theft) ke khilaf beginner-level standard protection lagayi hai."

### Live (production) par daalne se pehle
- Asli HTTPS certificate aur hosting.
- Admin password badlo, Gmail App Password hosting ke environment variables mein rakho.
- Database ka backup lo.
- Email confirmation, forgot password, 2-factor login aage ke improvements hain.
