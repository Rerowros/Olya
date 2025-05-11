namespace App1.Models
{
    public enum RoomStatus
    {
        Свободно,
        Занято,
        Уборка,
        ТехОбслуживание
    }

    public enum BookingStatus
    {
        Подтверждено,
        Проверено,
        Выписано,
        Отменено
    }

    public enum UserRole
    {
        Administrator,
        Receptionist
    }
}