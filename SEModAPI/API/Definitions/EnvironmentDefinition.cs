﻿using System;
using System.Collections.Generic;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using SEModAPI.Support;

namespace SEModAPI.API.Definitions
{
    public class EnvironmentDefinition : OverLayerDefinition<MyObjectBuilder_EnvironmentDefinition>
	{
		#region "Constructors and Initializers"

		public EnvironmentDefinition(MyObjectBuilder_EnvironmentDefinition definition): base(definition)
		{}

		#endregion


        #region "Methods"

        protected override string GetNameFrom(MyObjectBuilder_EnvironmentDefinition definition)
        {
            return null;
        }

        #endregion
    }
}