using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Itp.Win32.MdiHook.IPC
{
    [ComVisible(true)]
    public struct HookRegistrationRecord
    {
        public IntPtr HWnd;

        [MarshalAs(UnmanagedType.BStr)]
        public string HookFullyQualifiedTypeName;

        [MarshalAs(UnmanagedType.BStr)]
        public string HookCodebase;

        public Type HookType
        {
            get { return GetTypeForString(HookFullyQualifiedTypeName, HookCodebase); }
            private set
            {
                Contract.Requires(value != null);

                HookFullyQualifiedTypeName = value.AssemblyQualifiedName;
                HookCodebase = value.Assembly.Location;
            }
        }

        [MarshalAs(UnmanagedType.BStr)]
        public string ParameterFullyQualifiedTypeName;

        [MarshalAs(UnmanagedType.BStr)]
        public string ParameterCodebase;

        public Type ParameterType
        {
            get { return GetTypeForString(ParameterFullyQualifiedTypeName, ParameterCodebase); }
            private set
            {
                if (value == null)
                {
                    return;
                }

                ParameterFullyQualifiedTypeName = value.AssemblyQualifiedName;
                ParameterCodebase = value.Assembly.Location;
            }
        }

        [MarshalAs(UnmanagedType.BStr)]
        public string ParameterData;

        public object Parameter
        {
            get
            {
                if (ParameterData == null)
                {
                    return null;
                }

                var dcs = new DataContractSerializer(ParameterType);
                using (var sr = new StringReader(ParameterData))
                using (var xr = XmlDictionaryReader.CreateDictionaryReader(XmlReader.Create(sr)))
                {
                    return dcs.ReadObject(xr);
                }
            }
            private set
            {
                if (value == null)
                {
                    return;
                }
                if (ParameterFullyQualifiedTypeName == null)
                {
                    ParameterType = value.GetType();
                }

                var dcs = new DataContractSerializer(ParameterType);
                using (var sw = new StringWriter())
                using (var xw = XmlDictionaryWriter.CreateDictionaryWriter(XmlWriter.Create(sw)))
                {
                    dcs.WriteObject(xw, value);
                    xw.Flush();

                    ParameterData = sw.ToString();
                }
            }
        }

        public IMessageHookHandler<object> CreateHandler()
        {
            var tHandler = HookType;
            var tParam = ParameterType;
            if (tParam == null)
            {
                return (IMessageHookHandler<object>)Activator.CreateInstance(tHandler);
            }

            foreach (var ctor in tHandler.GetConstructors())
            {
                var paramList = ctor.GetParameters();
                if (paramList.Length != 1)
                {
                    continue;
                }
                var tCtorParam = paramList.Single().ParameterType;
                if (!tCtorParam.IsAssignableFrom(tParam))
                {
                    continue;
                }

                return (IMessageHookHandler<object>)ctor.Invoke(new[] { Parameter });
            }

            throw new NotSupportedException("No suitable constructor found");
        }

        [MarshalAs(UnmanagedType.BStr)]
        public string ResultFullyQualifiedTypeName;

        [MarshalAs(UnmanagedType.BStr)]
        public string ResultCodebase;

        public Type ResultType
        {
            get { return GetTypeForString(ResultFullyQualifiedTypeName, ResultCodebase); }
            private set
            {
                Contract.Requires(value != null);

                ResultFullyQualifiedTypeName = value.AssemblyQualifiedName;
                ResultCodebase = value.Assembly.Location;
            }
        }

        private static Type GetTypeForString(string fqtn, string codebase)
        {
            if (fqtn == null)
            {
                return null;
            }

            var t = Type.GetType(fqtn, throwOnError: false);
            if (t == null)
            {
                var loc = Path.GetDirectoryName(codebase);
                AssemblySearchPath.AddLoadLocation(loc);
                Assembly.LoadFrom(codebase);
            }
            return Type.GetType(fqtn, throwOnError: true);
        }

        public static HookRegistrationRecord Create(IntPtr hWnd, Type tHook, Type tResult, object param)
        {
            return new HookRegistrationRecord()
            {
                HWnd = hWnd,
                HookType = tHook,
                ResultType = tResult,
                Parameter = param
            };
        }
    }
}
