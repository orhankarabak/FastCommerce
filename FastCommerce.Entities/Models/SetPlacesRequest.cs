﻿using FastCommerce.Entities.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace FastCommerce.Entities.Models
{
    public class SetPlacesRequest
    {
        public int ProductId { get; set; }
        public int[] PlacementIds { get; set; }
    }
}
