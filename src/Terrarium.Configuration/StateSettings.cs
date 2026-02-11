// Copyright (c) Microsoft Corporation.  All rights reserved.

namespace Terrarium.Configuration;

/// <summary>
/// Enumerates and validates US states for peer teleportation metadata.
/// </summary>
public static class StateSettings
{
    public static readonly string[] States =
    [
        "Alabama",              "Alaska",           "Arizona",          "Arkansas",
        "California",           "Colorado",         "Connecticut",      "Delaware",
        "District of Columbia", "Florida",          "Georgia",          "Hawaii",
        "Idaho",                "Illinois",         "Indiana",          "Iowa",
        "Kansas",               "Kentucky",         "Louisiana",        "Maine",
        "Maryland",             "Massachusetts",    "Michigan",         "Minnesota",
        "Mississippi",          "Missouri",         "Montana",          "Nebraska",
        "Nevada",               "New Hampshire",    "New Jersey",       "New Mexico",
        "New York",             "North Carolina",   "North Dakota",     "Ohio",
        "Oklahoma",             "Oregon",           "Pennsylvania",     "Rhode Island",
        "South Carolina",       "South Dakota",     "Tennessee",        "Texas",
        "Utah",                 "Vermont",          "Virginia",         "Washington",
        "West Virginia",        "Wisconsin",        "Wyoming",          "<Unknown>"
    ];

    /// <summary>
    /// Returns true if the given string matches one of the 50 US states (+ DC + Unknown).
    /// </summary>
    public static bool Validate(string state)
        => Array.Exists(States, s => s == state);
}
