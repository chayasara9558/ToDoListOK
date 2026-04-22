# שלב 1: בניית ה-React
FROM node:20 AS node-build
WORKDIR /frontend

# תיקון נתיב: הוספת התיקייה הפנימית שבה נמצאים קבצי ה-npm
COPY ToDoListReact-master/ToDoListReact-master/package*.json ./
RUN npm install

# העתקת כל קוד המקור של ה-Frontend ובנייתו
COPY ToDoListReact-master/ToDoListReact-master/ ./
RUN npm run build

# שלב 2: בניית ה-.NET API
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# העתקת קובץ הפרויקט של ה-API
COPY TodoApi/*.csproj ./
RUN dotnet restore

# העתקת קוד ה-API וביצוע Publish
COPY TodoApi/ ./
RUN dotnet publish -c Release -o out

# שלב 3: יצירת התמונה הסופית להרצה
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

# העתקת התוצרים המוכנים
COPY --from=build /app/out .

# העתקת ה-Frontend הבנוי לתיקיית הסטטיקה של השרת
# ודאי שבפרויקט ה-React שלך תיקיית הפלט היא 'build' (ולא 'dist')
COPY --from=node-build /frontend/build ./wwwroot

# הגדרות פורט וסביבה עבור Render
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "TodoApi.dll"]
