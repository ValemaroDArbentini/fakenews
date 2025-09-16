\# 🧱 Fake News Game (Delirium TV News)



\*\*Fake News Game\*\* is a compact browser game built with C# (.NET 8), React, PostgreSQL, and Docker — developed as a personal pet project to combine fun gameplay, visual humor, and clean architecture.



\## 🎯 Concept



Inspired by the mechanics of block puzzles and wordplay, the game invites players to align falling \*words\* into rows — each word is a real noun or verb. Once a row is fully filled, it "burns", and the resulting pseudo-news phrase is broadcasted like a breaking headline.



The core idea: even absurdity can be entertaining if wrapped in a consistent UX.



\### 🧠 Design Highlights



\- \*\*Backend:\*\* ASP.NET Core Web API with PostgreSQL and EF Core

\- \*\*Frontend:\*\* Vite + React + TailwindCSS

\- \*\*Game logic:\*\* fully server-driven, deterministic outcomes

\- \*\*Deployment:\*\* Dockerized, ready for AWS ECS or local dev

\- \*\*Language-aware:\*\* supports multiple locales with lexeme dictionaries (e.g., Russian, English)

\- \*\*Persistence:\*\* sessions, burn events, user-saved headlines



\## 🔍 Example Gameplay



> "🗞️ BREAKING: спирт газ ухо"  

> "📡 эфир окончен. они там совсем поехали?"



Yes, it's meant to be ridiculous — and that's the point.



\## 🚀 Status



This is a work in progress but already functional:

\- ✅ Game session creation

\- ✅ Figure spawns \& moves

\- ✅ Row burning, scoring, combo chains

\- ✅ Swagger UI for API testing

\- ✅ Docker-ready



\## 🧪 Try it locally



```bash

git clone https://github.com/ValemaroDArbentini/fakenews.git

cd fakenews

./docker.bat up



Visit http://\_\_\_\_\_ and play via the browser. The API is also accessible via Swagger at /swagger.



❤️ About



This is a personal project I've spent several months crafting during evenings and weekends. I hope you find it interesting, insightful, or at least amusing.



– Valemaro d'Arbentini

