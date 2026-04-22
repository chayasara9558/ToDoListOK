# שלב 1: בניית ה-React
FROM node:20 AS node-build
WORKDIR /frontend

# העתקה מהנתיב הכפול כפי שמופיע במבנה התיקיות
COPY ToDoListReact-master/ToDoListReact-master/package*.json ./
RUN npm install

# העתקת כל שאר קבצי המקור של ה-Frontend
COPY ToDoListReact-master/ToDoListReact-master/ ./
RUN npm run build

# שלב 2: בניית ה-.NET API
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# בניית ה-API מתוך תיקיית TodoApi
COPY TodoApi/*.csproj ./
RUN dotnet restore

COPY TodoApi/ ./
RUN dotnet publish -c Release -o out

# שלב 3: יצירת התמונה הסופית להרצה
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

# העתקת ה-API המפורסם
COPY --from=build /app/out .

# העתקת ה-Frontend המקומפל לתיקיית wwwroot
# ודאי בתיקיית המקור אם הפלט הוא build או dist (לרוב ב-React זה build)
COPY --from=node-build /frontend/build ./wwwroot

# הגדרות פורט עבור Render
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "TodoApi.dll"]
