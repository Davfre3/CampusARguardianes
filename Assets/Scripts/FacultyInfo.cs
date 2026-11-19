using System;
using UnityEngine;

// Datos de una facultad. Se llenan desde el Inspector del ARGameManager.
[Serializable]
public class FacultyInfo
{
    public string id;               // "administracion", "ingenieria", "economia", "ciencias"
    public string nombreFacultad;   // "Facultad de Ingeniería"
    public string mascota;          // "Vikingo"
    [TextArea(2, 6)] public string descripcion;
    [TextArea(2, 6)] public string carreras;
    [TextArea(2, 6)] public string eventos;

    [Header("Reto")]
    [TextArea(1, 3)] public string pregunta;
    public string[] opciones = new string[3];
    public int respuestaCorrecta;   // 0, 1 o 2
}
