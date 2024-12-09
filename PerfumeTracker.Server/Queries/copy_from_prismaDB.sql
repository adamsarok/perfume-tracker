
CREATE EXTENSION dblink;

INSERT INTO "Perfume" ("Id", "House", "PerfumeName", "Rating", "Notes", "Ml", "ImageObjectKey", "Autumn", "Spring", "Summer",
	"Winter",  "Created_At", "Updated_At")
SELECT * FROM dblink('dbname=perfumetracker host=IPADDRESS port=5432 user=postgres password=PASSWORD',
                     'SELECT id, house, perfume, rating, notes, ml, "imageObjectKey", autumn, spring, summer,
	winter, "Created_At", "Updated_At"
 FROM public."Perfume"')
AS t(id integer, house text, perfume text, rating float8, notes text, ml int4, "imageObjectKey" text,
	autumn bool, spring bool, summer bool, winter bool, "Created_At" timestamptz,
	Updated_At timestamptz
);

select * from "Perfume" p 

INSERT INTO "PerfumeSuggested" ("Id", "PerfumeId", "Created_At")
SELECT * FROM dblink('dbname=perfumetracker host=IPADDRESS port=5432 user=postgres password=PASSWORD',
                     'SELECT "id", "perfumeId", "suggestedOn"
 FROM public."PerfumeSuggested"')
AS t(id integer, perfumeId integer, "suggestedOn" timestamp(3));

select * from "PerfumeSuggested" p 

INSERT INTO "Tag" ("Id", "TagName" , "Color" , "Created_At", "Updated_At")
SELECT * FROM dblink('dbname=perfumetracker host=IPADDRESS port=5432 user=postgres password=PASSWORD',
                     'SELECT "id", "tag", "color", "Created_At", "Updated_At"
 FROM public."Tag"')
AS t(id integer, tag text, "color" text, "Created_At" timestamptz, "Updated_At" timestamptz);

select * from "Tag" p 


INSERT INTO "PerfumeTag" ("Id", "PerfumeId", "TagId", "Created_At")
SELECT * FROM dblink('dbname=perfumetracker host=IPADDRESS port=5432 user=postgres password=PASSWORD',
                     'SELECT "id", "perfumeId", "tagId", "Created_At"
 FROM public."PerfumeTag"')
AS t(id integer, perfumeId integer, "tagId" integer, "Created_At" timestamptz);

select * from "PerfumeTag" p 


INSERT INTO "PerfumeWorn" ("Id", "PerfumeId", "Created_At")
SELECT * FROM dblink('dbname=perfumetracker host=IPADDRESS port=5432 user=postgres password=PASSWORD',
                     'SELECT "id", "perfumeId", "wornOn"
 FROM public."PerfumeWorn"')
AS t(id integer, perfumeId integer, "wornOn" timestamp(3));

select * from "PerfumeWorn" p 

INSERT INTO "Recommendation" ("Id", "Query", "Recommendations", "Created_At")
SELECT * FROM dblink('dbname=perfumetracker host=IPADDRESS port=5432 user=postgres password=PASSWORD',
                     'SELECT "id", "query", "recommendations", date
 FROM public."Recommendation"')
AS t(id integer, query text, recommendations text, "date" timestamp(3));


SELECT setval('public."PerfumeSuggested_Id_seq"', (SELECT MAX("Id") FROM "PerfumeSuggested") + 1);
SELECT setval('public."PerfumeTag_Id_seq"', (SELECT MAX("Id") FROM "PerfumeTag") + 1);
SELECT setval('public."PerfumeWorn_Id_seq"', (SELECT MAX("Id") FROM "PerfumeWorn") + 1);
SELECT setval('public."Perfume_Id_seq"', (SELECT MAX("Id") FROM "Perfume") + 1);
SELECT setval('public."Recommendation_Id_seq"', (SELECT MAX("Id") FROM "Recommendation" r) + 1);
SELECT setval('public."Tag_Id_seq"', (SELECT MAX("Id") FROM "Tag" t) + 1);