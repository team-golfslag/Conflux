The [Conflux.API]() project supports feature flags to enable/disable certain features.

| **Flag**              | **Function**                                                                                                                         | **Default** |
|-----------------------|--------------------------------------------------------------------------------------------------------------------------------------|:-----------:|
| Swagger               | Should enable a swagger endpoint at /swagger                                                                                         |    false    |
| SeedDatabase          | If set, will seed the database with NWOpen data if the database is not  yet populated                                                |    false    |
| NoDatabaseConnection  | If set, will not connect to a database. This will make the controllers unusable, but can be useful when you only want e.g. swagger   |    false    |