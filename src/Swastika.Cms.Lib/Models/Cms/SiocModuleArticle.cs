﻿using System;
using System.Collections.Generic;

namespace Swastika.Cms.Lib.Models.Cms
{
    public partial class SiocModuleArticle
    {
        public string ArticleId { get; set; }
        public int ModuleId { get; set; }
        public string Specificulture { get; set; }

        public SiocArticle SiocArticle { get; set; }
        public SiocModule SiocModule { get; set; }
    }
}