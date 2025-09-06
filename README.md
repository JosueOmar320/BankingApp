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

## Datos de prueba

### Clientes

| Cliente | Número de cuenta | Nombre       | Transacciones |
|---------|----------------|-------------|--------------|
| 1       | 1636126864     | Alice Smith | Varias      |
| 2       | 2110431022     | Bob Johnson | Ninguna       |

### Cuentas

- La **tasa de interés es fija del 10%** para todas las cuentas.

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


