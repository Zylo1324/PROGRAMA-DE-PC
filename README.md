# UnidorExacto

Aplicación de escritorio en WPF para unir archivos conservando los bytes exactos.

## Requisitos

- Windows 10/11
- .NET 8 SDK (LTS)

## Compilar

```bash
dotnet build UnidorExacto.sln
```

## Ejecutar

```bash
dotnet run --project UnidorExacto/UnidorExacto.csproj
```

## Uso

1. Pulsa **"Agregar archivos"** y selecciona los archivos que deseas unir.
2. Pulsa **"Unir y guardar"** y elige la ubicación de salida.
3. El archivo de salida conserva los bytes originales; si un archivo no termina en `\n`, se inserta `\r\n` antes de copiar el siguiente.
