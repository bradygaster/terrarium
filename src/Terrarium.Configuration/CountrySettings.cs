// Copyright (c) Microsoft Corporation.  All rights reserved.

namespace Terrarium.Configuration;

/// <summary>
/// Enumerates and validates countries/regions for peer teleportation metadata.
/// </summary>
public static class CountrySettings
{
    public static readonly string[] Countries =
    [
        "Afghanistan",          "Albania",          "Algeria",          "Andorra",
        "Angola",               "Antigua & Barbuda","Argentina",        "Armenia",
        "Australia",            "Austria",          "Azerbaijan",       "Bahamas, The",
        "Bahrain",              "Bangladesh",       "Barbados",         "Belarus",
        "Belgium",              "Belize",           "Benin",            "Bhutan",
        "Bolivia",              "Bosnia & Herz.",   "Botswana",         "Brazil",
        "Brunei",               "Bulgaria",         "Burkina Faso",     "Burundi",
        "Cote d'Ivoire",        "C.A.R.",           "Cambodia",         "Cameroon",
        "Canada",               "Cape Verde",       "Chad",             "Chile",
        "China",                "Colombia",         "Comoros",          "Congo (DRC)",
        "Costa Rica",           "Croatia",          "Cuba",             "Cyprus",
        "Czech Republic",       "Denmark",          "Djibouti",         "Dominica",
        "Dominican Rep.",       "Ecuador",          "Egypt",            "El Salvador",
        "Equatorial Guinea",    "Eritrea",          "Estonia",          "Ethiopia",
        "Fiji Islands",         "Finland",          "France",
        "Gabon",                "Gambia, The",      "Georgia",          "Germany",
        "Ghana",                "Greece",           "Grenada",          "Guatemala",
        "Guinea",               "Guinea-Bissau",    "Guyana",           "Haiti",
        "Honduras",             "Hungary",          "Iceland",          "India",
        "Indonesia",            "Iran",             "Iraq",             "Ireland",
        "Israel",               "Italy",            "Jamaica",          "Japan",
        "Jordan",               "Kazakhstan",       "Kenya",            "Kiribati",
        "Kuwait",               "Kyrgyzstan",       "Laos",             "Latvia",
        "Lebanon",              "Lesotho",          "Liberia",          "Libya",
        "Liechtenstein",        "Lithuania",        "Luxembourg",
        "Macedonia, Former Yugoslav Republic of",
        "Madagascar",           "Malawi",           "Malaysia",         "Maldives",
        "Mali",                 "Malta",            "Marshall Islands", "Mauritania",
        "Mauritius",
        "Mexico",               "Micronesia",       "Monaco",           "Mongolia",
        "Morocco",              "Mozambique",       "Myanmar",          "Namibia",
        "Nauru",                "Nepal",            "Netherlands, The",
        "New Zealand",          "Nicaragua",        "Niger",            "Nigeria",
        "North Korea",          "Norway",           "Oman",             "Pakistan",
        "Palau",                "Panama",           "Papua New Guinea", "Paraguay",
        "Peru",                 "Philippines",      "Poland",           "Portugal",
        "Qatar",                "Rep. of Congo",    "Republic of Moldova", "Romania",
        "Russia",               "Rwanda",           "Sao Tome & Prin.", "Samoa",
        "San Marino",           "Saudi Arabia",     "Senegal",          "Seychelles",
        "Sierra Leone",         "Singapore",        "Slovakiav",        "Slovenia",
        "Solomon Islands",      "Somalia",          "South Africa",     "South Korea",
        "Spain",                "Sri Lanka",        "St. Kitts & Nevis","St. Lucia",
        "St. Vincent & Gren.",  "Sudan",            "Suriname",         "Swaziland",
        "Sweden",               "Switzerland",      "Syria",            "Tajikistan",
        "Tanzania",             "Thailand",         "Togo",             "Tonga",
        "Trinidad & Tobago",    "Tunisia",          "Turkey",           "Turkmenistan",
        "Tuvalu",               "U.A.E.",           "Uganda",           "Ukraine",
        "United Kingdom",       "United States",    "Uruguay",          "Uzbekistan",
        "Vanuatu",              "Vatican City",     "Venezuela",        "Vietnam",
        "Yemen",                "Zambia",           "Zimbabwe",         "<Unknown>"
    ];

    /// <summary>
    /// Returns true if the given string matches a known country/region exactly.
    /// </summary>
    public static bool Validate(string country)
        => Array.Exists(Countries, c => c == country);
}
