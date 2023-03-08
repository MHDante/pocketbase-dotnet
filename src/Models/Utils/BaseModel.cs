using System;
using System.Collections.Generic;

namespace PocketBase.Net.SDK.Models.Utils
{
    public abstract class BaseModel
    {
        public string Id { get; set; } = "";
        public string Created { get; set; } = "";
        public string Updated { get; set; } = "";

        /**
         * Returns whether the current loaded data represent a stored db record.
         */
        public bool IsNew => string.IsNullOrEmpty(Id);

    }
}
