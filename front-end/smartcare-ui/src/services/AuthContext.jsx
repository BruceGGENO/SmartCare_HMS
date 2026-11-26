import React, { createContext, useContext, useState, useEffect } from 'react';
import api from '../services/api';

const AuthContext = createContext(null);

export const AuthProvider = ({ children }) => {
  const [user, setUser] = useState(() => {
    const saved = localStorage.getItem('smartcare_user');
    return saved ? JSON.parse(saved) : null;
  });
  const [token, setToken] = useState(() => localStorage.getItem('smartcare_token'));
  const [loading, setLoading] = useState(false);

  const login = async (userName, password) => {
    setLoading(true);
    try {
      const res = await api.post('/auth/login', { userName, password });
      const jwtToken = res.data.accessToken || res.data.token;
      const userProfile = res.data.user;
      
      localStorage.setItem('smartcare_token', jwtToken);
      localStorage.setItem('smartcare_user', JSON.stringify(userProfile));
      
      setToken(jwtToken);
      setUser(userProfile);
      return { success: true };
    } catch (err) {
      const msg = err.response?.data?.message || 'Login failed. Please check your credentials.';
      return { success: false, message: msg };
    } finally {
      setLoading(false);
    }
  };

  const logout = () => {
    localStorage.removeItem('smartcare_token');
    localStorage.removeItem('smartcare_user');
    setToken(null);
    setUser(null);
    window.location.href = '/login';
  };

  const hasPermission = (permissionCode) => {
    if (!user || !user.permissions) return false;
    return user.permissions.includes(permissionCode);
  };

  return (
    <AuthContext.Provider value={{ user, token, loading, login, logout, hasPermission }}>
      {children}
    </AuthContext.Provider>
  );
};

export const useAuth = () => useContext(AuthContext);
