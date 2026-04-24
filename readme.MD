Project: Case mangemet application.

#Contains 
Server WebAppMulti
UI - CaseManagementUI - React. or Angular.  Currently React.JS
   - other modules when new feature become availabe.
Database: SQL Server - can be [PostgreSQL, MongoDB, MySQL] in the future.
Other: can deploy to Docker.
         Can be run from home server exposed to a few until  go live.
Can be micro serviced each feature cna be its own standalone

# Case Management Application

A modern, modular case management platform designed for scalability, flexibility, and future growth.

## Overview

This repository contains the full Case Management Application, including:

- **Server API**: `WebAppMulti`
- **User Interface**: `CaseManagementUI`
  - Currently built with React
  - Angular support is planned as an alternative frontend option
- **Additional Modules**
  - New features can be developed and added as independent modules over time

## Technology Stack

### Backend
- .NET / ASP.NET Core (`WebAppMulti`)

### Frontend
- React (current implementation)
- Angular (planned option)

### Database
- SQL Server (current)
- Future database support may include:
  - PostgreSQL
  - MongoDB
  - MySQL

## Deployment Options

- Docker containerization supported
- Suitable for:
  - Local development
  - Home server hosting
  - Small private deployments
  - Cloud deployment for production

## Architecture

The application is designed with a modular architecture that supports:

- Monolithic deployment for simplicity
- Microservices evolution as the platform grows
- Independent feature modules
- Scalable backend services
- Flexible frontend implementations

Each major feature can eventually be deployed as its own standalone service if needed.

## Repository Structure
- see git



