using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Taunt
{
    public int id;
    public string text;
    public string taunt;
    public string mensaje;

    public Taunt(int id, string tauntText)
    {
        this.id = id;
        this.taunt = tauntText;
    }

    public string GetMessage()
    {
        if (!string.IsNullOrEmpty(taunt)) return taunt;
        if (!string.IsNullOrEmpty(text)) return text;
        if (!string.IsNullOrEmpty(mensaje)) return mensaje;
        return "¡Burla!";
    }
}