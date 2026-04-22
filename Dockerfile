# שלב 1: בניית ה-React
FROM node:20 AS node-build
WORKDIR /frontend
# העתקת קבצי הגדרות ה-npm
COPY ToDoListReact-master/package*.json ./
RUN npm install
# העתקת כל קוד הלקוח ובנייתו
COPY ToDoListReact-master/ ./
RUN npm run build

# שלב 2: בניית ה-.NET API
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# העתקת קובץ הפרויקט מתוך תיקיית TodoApi
COPY TodoApi/*.csproj ./
RUN dotnet restore

# העתקת כל קוד ה-API ובנייתו
COPY TodoApi/ ./
RUN dotnet publish -c Release -o out

# שלב 3: יצירת התמונה הסופית להרצה
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

# העתקת ה-API הבנוי
COPY --from=build /app/out .

# העתקת ה-React הבנוי לתוך תיקיית wwwroot של ה-API
COPY --from=node-build /frontend/build ./wwwroot

# הגדרות פורט וסביבה
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "TodoApi.dll"]
