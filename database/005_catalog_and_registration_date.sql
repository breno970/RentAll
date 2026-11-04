-- Non-destructive catalog expansion and a reliable registration timestamp for new accounts.
-- Legacy users retain NULL because their historical registration date is not available.
ALTER TABLE rentall."AspNetUsers" ADD COLUMN IF NOT EXISTS "CreatedAt" timestamptz;

INSERT INTO rentall."Categorias" (nmcategoria)
SELECT x.name FROM (VALUES
 ('Fotografia e vídeo'), ('Tecnologia e informática'), ('Bebés e crianças'),
 ('Livros e cultura'), ('Mobilidade e transportes'), ('Casa e jardim'),
 ('Festas e eventos'), ('Campismo e natureza'), ('Desporto e fitness'),
 ('Construção e bricolage'), ('Cozinha e catering'), ('Moda e acessórios'),
 ('Animais de estimação'), ('Escritório e trabalho'), ('Saúde e bem-estar'),
 ('Viagem e bagagem'), ('DJ, iluminação e som'), ('Brinquedos e jogos'),
 ('Indústria e equipamentos'), ('Outros')
) AS x(name)
WHERE NOT EXISTS (SELECT 1 FROM rentall."Categorias" c WHERE lower(c.nmcategoria) = lower(x.name));

WITH names(category, type_name) AS (
 VALUES
 ('VideoGame','Consolas'), ('VideoGame','Jogos'), ('VideoGame','Comandos'), ('VideoGame','Realidade virtual'),
 ('Áudio e Vídeo','Projetores'), ('Áudio e Vídeo','Televisões'), ('Áudio e Vídeo','Sistemas de som'), ('Áudio e Vídeo','Colunas'),
 ('Música e Hobbies','Guitarras'), ('Música e Hobbies','Pianos e teclados'), ('Música e Hobbies','Instrumentos de sopro'), ('Música e Hobbies','Baterias'),
 ('Esporte e Ginástica','Bicicletas'), ('Esporte e Ginástica','Máquinas de exercício'), ('Esporte e Ginástica','Material desportivo'),
 ('Ferramentas','Berbequins e aparafusadoras'), ('Ferramentas','Serras'), ('Ferramentas','Rebarbadoras'), ('Ferramentas','Lavadoras de pressão'), ('Ferramentas','Ferramentas manuais'),
 ('Eletrodomésticos','Máquinas de lavar'), ('Eletrodomésticos','Aspiradores'), ('Eletrodomésticos','Frigoríficos'), ('Eletrodomésticos','Eletrodomésticos pequenos'),
 ('Fotografia e vídeo','Câmaras'), ('Fotografia e vídeo','Objetivas'), ('Fotografia e vídeo','Tripés'), ('Fotografia e vídeo','Drones'), ('Fotografia e vídeo','Estabilizadores'),
 ('Tecnologia e informática','Computadores portáteis'), ('Tecnologia e informática','Monitores'), ('Tecnologia e informática','Tablets'), ('Tecnologia e informática','Impressoras'), ('Tecnologia e informática','Redes e acessórios'),
 ('Bebés e crianças','Carrinhos de bebé'), ('Bebés e crianças','Cadeiras auto'), ('Bebés e crianças','Berços'), ('Bebés e crianças','Artigos infantis'),
 ('Livros e cultura','Livros'), ('Livros e cultura','Instrumentos educativos'), ('Livros e cultura','Material artístico'),
 ('Mobilidade e transportes','Bicicletas e trotinetes'), ('Mobilidade e transportes','Acessórios automóveis'), ('Mobilidade e transportes','Reboques'),
 ('Casa e jardim','Mobiliário'), ('Casa e jardim','Equipamento de jardim'), ('Casa e jardim','Limpeza'), ('Casa e jardim','Decoração'),
 ('Festas e eventos','Mesas e cadeiras'), ('Festas e eventos','Tendas'), ('Festas e eventos','Decoração de eventos'), ('Festas e eventos','Cabines fotográficas'),
 ('Campismo e natureza','Tendas de campismo'), ('Campismo e natureza','Mochilas'), ('Campismo e natureza','Material de caminhada'), ('Campismo e natureza','Equipamento de praia'),
 ('Desporto e fitness','Fitness'), ('Desporto e fitness','Desportos aquáticos'), ('Desporto e fitness','Desportos de inverno'), ('Desporto e fitness','Equipamento de equipa'),
 ('Construção e bricolage','Andaimes'), ('Construção e bricolage','Máquinas de obra'), ('Construção e bricolage','Escadas'), ('Construção e bricolage','Medição'),
 ('Cozinha e catering','Máquinas de café'), ('Cozinha e catering','Cozinha profissional'), ('Cozinha e catering','Utensílios'), ('Cozinha e catering','Serviço de mesa'),
 ('Moda e acessórios','Vestuário para ocasiões'), ('Moda e acessórios','Malas'), ('Moda e acessórios','Acessórios'), ('Moda e acessórios','Calçado'),
 ('Animais de estimação','Transportadoras'), ('Animais de estimação','Acessórios para animais'), ('Animais de estimação','Equipamentos'),
 ('Escritório e trabalho','Mesas de escritório'), ('Escritório e trabalho','Cadeiras ergonómicas'), ('Escritório e trabalho','Equipamento de reunião'),
 ('Saúde e bem-estar','Equipamento de mobilidade'), ('Saúde e bem-estar','Equipamento de bem-estar'), ('Saúde e bem-estar','Ajudas técnicas'),
 ('Viagem e bagagem','Malas de viagem'), ('Viagem e bagagem','Acessórios de viagem'), ('Viagem e bagagem','Porta-bagagens'),
 ('DJ, iluminação e som','Mesas de mistura'), ('DJ, iluminação e som','Colunas e PA'), ('DJ, iluminação e som','Microfones'), ('DJ, iluminação e som','Iluminação'),
 ('Brinquedos e jogos','Jogos de tabuleiro'), ('Brinquedos e jogos','Brinquedos educativos'), ('Brinquedos e jogos','Brinquedos de exterior'),
 ('Indústria e equipamentos','Ferramentas industriais'), ('Indústria e equipamentos','Equipamento de armazém'), ('Indústria e equipamentos','Equipamento de produção'),
 ('Outros','Diversos')
)
INSERT INTO rentall."Tipos" (nmtipo, categoriaid)
SELECT n.type_name, c.id
FROM names n JOIN rentall."Categorias" c ON c.nmcategoria = n.category
WHERE NOT EXISTS (
 SELECT 1 FROM rentall."Tipos" t
 WHERE t.categoriaid = c.id AND lower(t.nmtipo) = lower(n.type_name)
);

-- Always retain a fallback "Outros" for every category.
INSERT INTO rentall."Tipos" (nmtipo, categoriaid)
SELECT 'Outros', c.id FROM rentall."Categorias" c
WHERE NOT EXISTS (SELECT 1 FROM rentall."Tipos" t WHERE t.categoriaid = c.id AND lower(t.nmtipo) = 'outros');
