namespace SerialMonitor.Enumes;

public enum SerialDataBits
{
    Five = 5,
    Six = 6,
    Seven = 7,
    Eight = 8,
    Sixteen = 16
}

public enum SerialParity
{
    None = 0,
    Even = 1,
    Odd = 2,
    Mark = 3,
    Space = 4
}

public enum SerialStopBits
{
    One = 1,
    OnePointFive = 2,
    Two = 3
}