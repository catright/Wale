using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JDPack
{
    public static class FormPack2
    {
        public static void Bind(System.Windows.Forms.Control bindingTarget, string propertyName, object dataSource, string dataMember)
        {
            bindingTarget.DataBindings.Add(propertyName, dataSource, dataMember, true, System.Windows.Forms.DataSourceUpdateMode.OnValidation);
        }
    }// End class FormPack2
}// End namespace JDPack
