using System;
using System.Collections.Generic;
using System.Text;
using Toolbox.Core;
using Toolbox.Core.OpenGL;

namespace NextLevelLibrary
{
    public class ModelFormat : IModelFormat
    {
        public ModelRenderer Renderer => new LMRender(ToGeneric());

        private STGenericModel Model;

        public ModelFormat(STGenericModel model) {
            Model = model;
        }

        public STGenericModel ToGeneric() {
            return Model;
        }
    }
}
