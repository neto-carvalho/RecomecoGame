public static class CityStreetLightNaming
{
    /// <summary>
    /// Postes da Cidade: "Pole", "Pole(1)" … "Pole(484)" (e variantes com espaço).
    /// </summary>
    public static bool IsStreetPole(string objectName)
    {
        if (string.IsNullOrEmpty(objectName))
            return false;

        if (objectName.Equals("Pole", System.StringComparison.OrdinalIgnoreCase))
            return true;

        if (objectName.StartsWith("Pole(", System.StringComparison.OrdinalIgnoreCase) &&
            objectName.EndsWith(")", System.StringComparison.Ordinal))
            return true;

        if (objectName.StartsWith("Pole (", System.StringComparison.OrdinalIgnoreCase) &&
            objectName.EndsWith(")", System.StringComparison.Ordinal))
            return true;

        if (objectName.Equals("lamppost", System.StringComparison.OrdinalIgnoreCase))
            return true;

        if (objectName.StartsWith("Light_", System.StringComparison.OrdinalIgnoreCase))
            return true;

        if (objectName.Contains("Poste", System.StringComparison.OrdinalIgnoreCase))
            return true;

        return objectName.Contains("LampPost", System.StringComparison.OrdinalIgnoreCase);
    }
}
