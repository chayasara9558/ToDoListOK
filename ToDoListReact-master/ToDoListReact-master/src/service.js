import axios from 'axios';

const apiUrl = ""// 1. הזרקת הטוקן לכל בקשה שיוצאת לשרת
axios.interceptors.request.use(config => {
    const token = localStorage.getItem("token");
    if (token) {
        config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
});

// 2. טיפול בשגיאות חוזרות מהשרת
axios.interceptors.response.use(
    response => response,
    error => {
        // אם השרת מחזיר 401, זה אומר שאין טוקן או שהוא פג תוקף
        if (error.response && error.response.status === 401) {
            console.warn("User is not authorized - redirecting to login");
            localStorage.removeItem("token"); // ננקה את מה שיש
            
            // במקום לזרוק שגיאה שתקריס את האתר, נעביר דף
            if (!window.location.pathname.includes("/login")) {
                window.location.href = "/login";
            }
        }
        return Promise.reject(error);
    }
);

export default {
  // התחברות - מקבל טוקן ושומר אותו
  login: async (username, password) => {
    const result = await axios.post(`${apiUrl}/login`, { username, password });
    localStorage.setItem("token", result.data.token); 
    return result.data;
  },

  // הרשמה של משתמש חדש
  register: async (username, password) => {
    const result = await axios.post(`${apiUrl}/register`, { username, password });
    return result.data;
  },

  getTasks: async () => {
    const result = await axios.get(`${apiUrl}/items`);    
    return result.data;
  },

  addTask: async (name) => {
    const result = await axios.post(`${apiUrl}/items`, { name: name, isComplete: false });
    return result.data;
  },

  setCompleted: async (id, isComplete) => {
    const result = await axios.put(`${apiUrl}/items/${id}`, { isComplete: isComplete });
    return result.data;
  },

  deleteTask: async (id) => {
    const result = await axios.delete(`${apiUrl}/items/${id}`);
    return result.data;
  }
};
