

Query to get the paintings id:

```sql
SELECT ?item ?article1 ?pic ?name WHERE {
  ?item wdt:P31 wd:Q3305213; 
        wdt:P18 ?pic;
        wdt:P1476 ?name.
  FILTER (lang(?name) = "en")
  ?article1 schema:about ?item .
  ?article2 schema:about ?item .
  ?article3 schema:about ?item .
  ?article1 schema:isPartOf <https://en.wikipedia.org/>.
  ?article2 schema:isPartOf <https://fr.wikipedia.org/>.
  ?article3 schema:isPartOf <https://de.wikipedia.org/>.
}
```
