# BankingApp
# API de Banca en .NET 8

Este proyecto es una **API de banca** desarrollada en **.NET 8** usando **ASP.NET Web API**. Permite gestionar clientes, cuentas bancarias y transacciones (depósitos, retiros y cálculo de intereses).  

La base de datos ya está creada y contiene **cuentas de prueba**, por lo que solo necesitas ejecutar la API.

---

## Características

- Crear perfil de cliente
- Crear cuenta bancaria asociada a un cliente
- Consultar saldo de una cuenta
- Registrar depósitos y retiros
- Aplicar intereses sobre el saldo
- Consultar historial de transacciones y resumen del saldo final
- Validaciones de integridad: saldo inicial mayor a 0, retiro no mayor al saldo disponible
- Pruebas unitarias con **NUnit** y **Moq**
- Documentación Swagger integrada

---

### Cuentas

- La **tasa de interés es fija del 10%** para todas las cuentas.

## Requisitos previos
- [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) instalado
- Visual Studio, Visual Studio Code o cualquier editor compatible con .NET

## Ejecución del proyecto
1. Clonar el repositorio:
   ```bash
   git clone https://github.com/usuario/BankingApp.git
   cd BankingApp
2. Clonar el repositorio:
  ```bash
  dotnet restore
  ```

3. Ejecutar la API:
  ```bash
  dotnet run --project Banking.Api
  ```

La API se ejecutará en:
  ```bash
  https://localhost:7201
  http://localhost:5200
  ```
  
5. Abrir Swagger para probar los endpoints en el navegador:
  ```bash
   https://localhost:7201/swagger
   http://localhost:5200/swagger
  ```

## Pruebas Unitarias

- **Proyecto de pruebas:** `Banking.Tests`
- **Frameworks utilizados:** NUnit, Moq

## Cobertura de Pruebas

Las pruebas unitarias incluyen los siguientes escenarios:

- **Creación de clientes y cuentas**
- **Operaciones de depósito y retiro**
- **Aplicación de intereses** (tasa fija del 10%)
- **Consultas de saldo y resumen de transacciones**
- **Validación de errores:**
  - Fondos insuficientes
  - Montos negativos o cero
  - Validaciones de datos obligatorios

### Ejecutar todas las pruebas
```bash
dotnet test
```

### Ejecutar pruebas con detalles
```bash
dotnet test --logger "console;verbosity=detailed"
```






