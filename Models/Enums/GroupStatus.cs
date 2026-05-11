namespace Diplom_StudyHub.Models.Enums
{
    public enum GroupStatus
    {
        Open = 0,        // Открытая группа
        Closed = 1,      // Закрытая (только по приглашению)
        Archived = 2,    // Архив (только чтение)
        Banned = 3       // Заблокирована админом
    }
}