# שלב 1: בניית ה-React
FROM node:20 AS node-build
WORKDIR /frontend
# נניח שתיקיית הלקוח שלך נמצאת בתוך תיקיית הפרויקט
COPY todo-frontend/package*.json ./
RUN npm install
COPY todo-frontend/ ./
RUN npm run build

# שלב 2: בניית ה-.NET API
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# העתקת קבצי הפרויקט ושחזור תלויות
COPY *.csproj ./
RUN dotnet restore

# העתקה של כל קוד השרת
COPY . ./

# העתקת ה-Build של ה-React לתוך תיקיית wwwroot של השרת
# ודאי שבקוד ה-C# שלך יש app.UseStaticFiles()
COPY --from=node-build /frontend/build ./wwwroot

RUN dotnet publish -c Release -o out

# שלב 3: הרצה
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/out .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "TodoApi.dll"]
