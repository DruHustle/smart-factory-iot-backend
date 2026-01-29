-- ========================================
-- Smart Factory IoT Backend - Local Database Initialization
-- ========================================

CREATE DATABASE IF NOT EXISTS SmartFactory;
USE SmartFactory;

-- The actual table creation is handled by EF Core Migrations.
-- This script ensures the database exists and provides a place
-- for seed data if needed during local development.

-- Example seed data (optional)
-- INSERT INTO Devices (Id, Name, Type, Status) VALUES (UUID(), 'Factory-A-Node-1', 'ESP32', 'Active');
