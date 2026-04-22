# שלב 1: בניית ה-React (Frontend)
FROM node:20 AS node-build
WORKDIR /frontend

# העתקת קבצי הגדרות התלויות
COPY ToDoListReact-master/package*.json ./
RUN npm install

# העתקת כל קוד הלקוח ובנייתו
COPY ToDoListReact-master/ ./
RUN npm run build

# שלב 2: בניית ה-.NET API (Backend)
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# העתקת קובץ הפרויקט ושחזור חבילות NuGet
COPY *.csproj ./
RUN dotnet restore

# העתקת כל שאר קבצי השרת
COPY . ./

# העתקת תוצרי הבנייה של ה-React לתיקיית wwwroot של השרת
# ודאי שב-Program.cs מוגדר app.UseStaticFiles()
COPY --from=node-build /frontend/build ./wwwroot

RUN dotnet publish -c Release -o out

# שלב 3: יצירת תמונת ההרצה הסופית
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/out .

# חשיפת הפורט והרצת האפליקציה
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "TodoApi.dll"]
