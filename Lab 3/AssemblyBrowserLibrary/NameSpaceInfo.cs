﻿namespace AssemblyBrowserLibrary
{
    public class NamespaceInfo : ContainerInfo
    {
        public NamespaceInfo() : base()
        {
        }

        public override MemberType GetContainerType => MemberType.Namespace;
    }
}
