/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace TeamCloud.Model
{
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class User : Identifiable, IEquatable<User>
    {
        public Guid Id { get; set; }

        public string Role { get; set; }

        public Dictionary<string, string> Tags { get; set; } = new Dictionary<string, string>();

        public bool Equals(User other) => Id.Equals(other.Id);
    }
}