using System.Collections.Generic;

namespace AssemblyBrowserLibrary
{
    public abstract class ContainerInfo : Member
    {
        // Инициализация пустого списка сущностей пространства имен
        public ContainerInfo()
        {
            Members = new List<Member>();
        }
        // Сущности пространства имен
        public List<Member> Members { get; set; }
        // Добавление сущности пространства имен в общий список
        internal void AddMember(Member member)
        {
            Members.Add(member);
        }
    }
}
