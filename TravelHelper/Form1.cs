using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using System.IO;

namespace TravelHelper
{
    public partial class Form1 : Form
    {
        private Panel mapPanel;
        private Panel infoPanel;
        private Panel notesPanel;
        private DataGridView notesGrid;
        private TextBox noteTitleBox, noteTextBox;
        private PictureBox notePictureBox;
        private Button addNoteBtn, deleteNoteBtn, uploadImageBtn;
        private ComboBox countryCombo;
        private Label countryNameLbl, capitalLbl, languageLbl, currencyLbl, citiesLbl;
        private List<CountryData> countriesData;
        private string currentCountry;
        private string dbPath = "travel_notes.db";
        private Image worldMapImage;

        // Словарь для хранения регионов стран (полигоны)
        private Dictionary<string, List<Point>> countryRegions;

        public Form1()
        {
            InitializeComponent();
            this.Icon = CreateAppIcon();
            LoadWorldMapFromResources();
            SetupForm();
            CreateDatabase();
            LoadCountriesData();
            InitializeCountryRegions();

            // Убираем автоматическое открытие окна заметок
            // this.Shown += (s, e) => ShowNotesWindow();
        }

        private void LoadWorldMapFromResources()
        {
            try
            {
                worldMapImage = Properties.Resources.Map_politic;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось загрузить карту из ресурсов: {ex.Message}");
            }
        }

        private Icon CreateAppIcon()
        {
            Bitmap bitmap = new Bitmap(64, 64);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.Transparent);
                g.SmoothingMode = SmoothingMode.AntiAlias;

                using (Brush globeBrush = new LinearGradientBrush(new Rectangle(8, 8, 48, 48),
                       Color.FromArgb(70, 130, 200), Color.FromArgb(25, 25, 112), LinearGradientMode.Vertical))
                {
                    g.FillEllipse(globeBrush, 8, 8, 48, 48);
                }

                using (Pen whitePen = new Pen(Color.White, 2))
                {
                    g.DrawEllipse(whitePen, 8, 8, 48, 48);
                    g.DrawLine(whitePen, 32, 8, 32, 56);
                    g.DrawLine(whitePen, 8, 32, 56, 32);
                }

                using (Brush continentBrush = new SolidBrush(Color.FromArgb(139, 69, 19)))
                {
                    g.FillEllipse(continentBrush, 20, 15, 15, 10);
                    g.FillEllipse(continentBrush, 35, 20, 12, 8);
                    g.FillEllipse(continentBrush, 25, 30, 10, 12);
                }

                using (Pen redPen = new Pen(Color.Red, 2))
                {
                    Point[] plane = { new Point(32, 28), new Point(50, 22), new Point(50, 34), new Point(32, 28) };
                    g.DrawPolygon(redPen, plane);
                    g.DrawLine(redPen, 32, 28, 18, 28);
                }
            }
            return Icon.FromHandle(bitmap.GetHicon());
        }

        private void InitializeCountryRegions()
        {
            countryRegions = new Dictionary<string, List<Point>>();

            // Определяем приблизительные координаты для каждой страны на карте
            // Координаты в пикселях относительно размера карты 1400x800

            // Россия (огромная территория)
            countryRegions["Россия"] = new List<Point>
            {
                new Point(600, 50), new Point(750, 50), new Point(850, 80), new Point(900, 120),
                new Point(920, 180), new Point(900, 240), new Point(850, 280), new Point(800, 300),
                new Point(750, 310), new Point(700, 300), new Point(650, 280), new Point(600, 250),
                new Point(580, 200), new Point(570, 150), new Point(580, 100), new Point(600, 50)
            };

            // США
            countryRegions["США"] = new List<Point>
            {
                new Point(150, 200), new Point(280, 190), new Point(350, 200), new Point(370, 230),
                new Point(360, 270), new Point(320, 290), new Point(260, 300), new Point(200, 290),
                new Point(160, 270), new Point(140, 240), new Point(150, 200)
            };

            // Канада
            countryRegions["Канада"] = new List<Point>
            {
                new Point(140, 80), new Point(280, 70), new Point(380, 80), new Point(400, 110),
                new Point(390, 140), new Point(350, 160), new Point(280, 160), new Point(200, 150),
                new Point(150, 140), new Point(130, 110), new Point(140, 80)
            };

            // Бразилия
            countryRegions["Бразилия"] = new List<Point>
            {
                new Point(280, 420), new Point(350, 400), new Point(400, 410), new Point(420, 440),
                new Point(410, 480), new Point(370, 500), new Point(320, 510), new Point(280, 490),
                new Point(260, 460), new Point(280, 420)
            };

            // Аргентина
            countryRegions["Аргентина"] = new List<Point>
            {
                new Point(290, 530), new Point(340, 520), new Point(370, 540), new Point(360, 580),
                new Point(330, 610), new Point(300, 600), new Point(280, 570), new Point(290, 530)
            };

            // Великобритания
            countryRegions["Великобритания"] = new List<Point>
            {
                new Point(480, 170), new Point(510, 165), new Point(525, 175), new Point(520, 190),
                new Point(500, 195), new Point(485, 185), new Point(480, 170)
            };

            // Франция
            countryRegions["Франция"] = new List<Point>
            {
                new Point(490, 230), new Point(530, 225), new Point(550, 235), new Point(545, 255),
                new Point(520, 260), new Point(500, 255), new Point(490, 240), new Point(490, 230)
            };

            // Германия
            countryRegions["Германия"] = new List<Point>
            {
                new Point(520, 190), new Point(560, 185), new Point(575, 195), new Point(570, 210),
                new Point(545, 215), new Point(525, 210), new Point(520, 190)
            };

            // Италия
            countryRegions["Италия"] = new List<Point>
            {
                new Point(530, 270), new Point(560, 265), new Point(575, 275), new Point(570, 295),
                new Point(550, 305), new Point(535, 295), new Point(530, 275), new Point(530, 270)
            };

            // Китай
            countryRegions["Китай"] = new List<Point>
            {
                new Point(700, 220), new Point(820, 200), new Point(880, 220), new Point(900, 260),
                new Point(880, 310), new Point(820, 330), new Point(760, 340), new Point(700, 330),
                new Point(680, 300), new Point(670, 260), new Point(700, 220)
            };

            // Индия
            countryRegions["Индия"] = new List<Point>
            {
                new Point(720, 350), new Point(780, 340), new Point(800, 360), new Point(790, 400),
                new Point(750, 420), new Point(710, 410), new Point(690, 380), new Point(720, 350)
            };

            // Япония
            countryRegions["Япония"] = new List<Point>
            {
                new Point(950, 270), new Point(980, 260), new Point(1000, 280), new Point(990, 320),
                new Point(960, 330), new Point(940, 310), new Point(950, 270)
            };

            // Австралия
            countryRegions["Австралия"] = new List<Point>
            {
                new Point(820, 550), new Point(920, 530), new Point(1000, 550), new Point(1010, 600),
                new Point(970, 640), new Point(900, 650), new Point(830, 630), new Point(800, 590),
                new Point(820, 550)
            };

            // ЮАР
            countryRegions["ЮАР"] = new List<Point>
            {
                new Point(520, 550), new Point(580, 540), new Point(620, 560), new Point(610, 600),
                new Point(570, 620), new Point(530, 610), new Point(510, 580), new Point(520, 550)
            };

            // Египет
            countryRegions["Египет"] = new List<Point>
            {
                new Point(520, 350), new Point(570, 340), new Point(590, 360), new Point(580, 390),
                new Point(550, 400), new Point(520, 390), new Point(510, 370), new Point(520, 350)
            };
        }

        // Метод для определения страны по координатам
        private string GetCountryAtPoint(int x, int y)
        {
            foreach (var country in countryRegions)
            {
                if (IsPointInPolygon(new Point(x, y), country.Value))
                {
                    return country.Key;
                }
            }
            return null;
        }

        // Алгоритм проверки принадлежности точки полигону (Ray casting algorithm)
        private bool IsPointInPolygon(Point point, List<Point> polygon)
        {
            bool inside = false;
            for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
            {
                if (((polygon[i].Y > point.Y) != (polygon[j].Y > point.Y)) &&
                    (point.X < (polygon[j].X - polygon[i].X) * (point.Y - polygon[i].Y) / (polygon[j].Y - polygon[i].Y) + polygon[i].X))
                {
                    inside = !inside;
                }
            }
            return inside;
        }

        private void SetupForm()
        {
            this.Text = "Travel Helper - Путешественник";
            this.Size = new Size(1400, 800);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(240, 248, 255);

            // Карта на весь экран
            mapPanel = new Panel
            {
                Location = new Point(0, 0),
                Size = this.ClientSize,
                BackColor = Color.FromArgb(135, 206, 250),
                BorderStyle = BorderStyle.None,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            mapPanel.Paint += MapPanel_Paint;
            mapPanel.MouseClick += MapPanel_MouseClick;

            // Скрытые панели (будут показываться при выборе страны)
            infoPanel = new Panel
            {
                Location = new Point(820, 10),
                Size = new Size(550, 280),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Visible = false
            };

            Label infoTitle = new Label
            {
                Text = "ИНФОРМАЦИЯ О СТРАНЕ",
                Location = new Point(10, 10),
                Size = new Size(530, 30),
                Font = new Font("Arial", 14, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.FromArgb(70, 130, 200),
                ForeColor = Color.White
            };

            countryNameLbl = CreateInfoLabel("Название: ", 50);
            capitalLbl = CreateInfoLabel("Столица: ", 90);
            languageLbl = CreateInfoLabel("Язык: ", 130);
            currencyLbl = CreateInfoLabel("Валюта: ", 170);
            citiesLbl = CreateInfoLabel("Города: ", 210);

            Button closeInfoBtn = new Button
            {
                Text = "Закрыть",
                Location = new Point(450, 245),
                Size = new Size(80, 25),
                BackColor = Color.FromArgb(231, 76, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Arial", 9, FontStyle.Bold)
            };
            closeInfoBtn.Click += (s, e) => infoPanel.Visible = false;

            infoPanel.Controls.AddRange(new Control[] { infoTitle, countryNameLbl, capitalLbl,
                                                         languageLbl, currencyLbl, citiesLbl, closeInfoBtn });

            notesPanel = new Panel
            {
                Location = new Point(820, 300),
                Size = new Size(550, 460),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Visible = false
            };

            Label notesTitle = new Label
            {
                Text = "ЗАМЕТКИ ПО СТРАНЕ",
                Location = new Point(10, 10),
                Size = new Size(530, 30),
                Font = new Font("Arial", 12, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.FromArgb(60, 179, 113),
                ForeColor = Color.White
            };

            Label selectCountryLbl = new Label
            {
                Text = "Выберите",
                Location = new Point(10, 50),
                Size = new Size(120, 25),
                Font = new Font("Arial", 10)
            };

            countryCombo = new ComboBox
            {
                Location = new Point(140, 50),
                Size = new Size(200, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            countryCombo.SelectedIndexChanged += CountryCombo_SelectedIndexChanged;

            notesGrid = new DataGridView
            {
                Location = new Point(10, 90),
                Size = new Size(530, 180),
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            notesGrid.SelectionChanged += NotesGrid_SelectionChanged;

            Label titleLbl = new Label
            {
                Text = "Название",
                Location = new Point(10, 285),
                Size = new Size(120, 25),
                Font = new Font("Arial", 10)
            };

            noteTitleBox = new TextBox
            {
                Location = new Point(140, 285),
                Size = new Size(400, 25),
                Font = new Font("Arial", 10)
            };

            Label textLbl = new Label
            {
                Text = "Текст заметки:",
                Location = new Point(10, 320),
                Size = new Size(120, 25),
                Font = new Font("Arial", 10)
            };

            noteTextBox = new TextBox
            {
                Location = new Point(140, 320),
                Size = new Size(400, 60),
                Multiline = true,
                Font = new Font("Arial", 10)
            };

            Label photoLbl = new Label
            {
                Text = "Фото:",
                Location = new Point(10, 395),
                Size = new Size(120, 25),
                Font = new Font("Arial", 10)
            };

            notePictureBox = new PictureBox
            {
                Location = new Point(140, 395),
                Size = new Size(80, 80),
                BorderStyle = BorderStyle.FixedSingle,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.LightGray
            };

            uploadImageBtn = new Button
            {
                Text = "Загрузить",
                Location = new Point(240, 420),
                Size = new Size(95, 30),
                BackColor = Color.FromArgb(52, 152, 219),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Arial", 9, FontStyle.Bold)
            };
            uploadImageBtn.Click += UploadImageBtn_Click;

            addNoteBtn = new Button
            {
                Text = "Добавить",
                Location = new Point(345, 420),
                Size = new Size(95, 30),
                BackColor = Color.FromArgb(46, 204, 113),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Arial", 9, FontStyle.Bold)
            };
            addNoteBtn.Click += AddNoteBtn_Click;

            deleteNoteBtn = new Button
            {
                Text = "Удалить",
                Location = new Point(450, 420),
                Size = new Size(95, 30),
                BackColor = Color.FromArgb(231, 76, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Arial", 9, FontStyle.Bold)
            };
            deleteNoteBtn.Click += DeleteNoteBtn_Click;

            Button closeNotesBtn = new Button
            {
                Text = "Закрыть",
                Location = new Point(450, 425),
                Size = new Size(80, 25),
                BackColor = Color.FromArgb(231, 76, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Arial", 9, FontStyle.Bold)
            };
            closeNotesBtn.Click += (s, e) => notesPanel.Visible = false;

            notesPanel.Controls.AddRange(new Control[] { notesTitle, selectCountryLbl, countryCombo,
                                                           notesGrid, titleLbl, noteTitleBox, textLbl,
                                                           noteTextBox, photoLbl, notePictureBox,
                                                           uploadImageBtn, addNoteBtn, deleteNoteBtn, closeNotesBtn });

            this.Controls.AddRange(new Control[] { mapPanel, infoPanel, notesPanel });

            this.Resize += (s, e) =>
            {
                mapPanel.Size = this.ClientSize;
                mapPanel.Invalidate();
            };
        }

        private Label CreateInfoLabel(string prefix, int y)
        {
            Label label = new Label
            {
                Text = prefix,
                Location = new Point(20, y),
                Size = new Size(510, 30),
                Font = new Font("Arial", 11),
                BackColor = Color.FromArgb(248, 248, 255)
            };
            return label;
        }

        private void MapPanel_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;

            // Рисуем политическую карту мира из ресурсов на весь экран
            if (worldMapImage != null)
            {
                g.DrawImage(worldMapImage, 0, 0, mapPanel.Width, mapPanel.Height);
            }
            else
            {
                // Если карта не загружена, заливаем океан
                using (LinearGradientBrush oceanBrush = new LinearGradientBrush(
                       new Rectangle(0, 0, mapPanel.Width, mapPanel.Height),
                       Color.FromArgb(135, 206, 250),
                       Color.FromArgb(25, 25, 112),
                       LinearGradientMode.Vertical))
                {
                    g.FillRectangle(oceanBrush, mapPanel.ClientRectangle);
                }
            }

            using (Pen wavePen = new Pen(Color.FromArgb(80, 255, 255, 255), 1))
            {
                for (int i = 0; i < 15; i++)
                {
                    int y = 40 + i * 40;
                    g.DrawCurve(wavePen, new Point[] {
                        new Point(0, y), new Point(100, y - 4), new Point(200, y + 2),
                        new Point(300, y - 3), new Point(400, y + 1), new Point(500, y - 2),
                        new Point(600, y + 3), new Point(700, y - 1), new Point(800, y)
                    });
                }
            }

            using (Font oceanFont = new Font("Arial", 9, FontStyle.Italic))
            using (Brush oceanTextBrush = new SolidBrush(Color.FromArgb(180, 255, 255, 255)))
            {
                g.DrawString("Тихий океан", oceanFont, oceanTextBrush, 20, 280);
                g.DrawString("Атлантический\nокеан", oceanFont, oceanTextBrush, 180, 380);
                g.DrawString("Индийский океан", oceanFont, oceanTextBrush, 520, 480);
                g.DrawString("Северный\nЛедовитый\nокеан", oceanFont, oceanTextBrush, 350, 30);
            }

            using (Pen equatorPen = new Pen(Color.FromArgb(100, 255, 100, 100), 2))
            using (Pen dashPen = new Pen(Color.FromArgb(150, 255, 255, 255), 1))
            {
                dashPen.DashPattern = new float[] { 5, 5 };
                g.DrawLine(equatorPen, 0, 300, 800, 300);
                g.DrawLine(dashPen, 0, 300, 800, 300);
            }
        }

        private void MapPanel_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                // Показываем контекстное меню со списком всех стран
                ContextMenuStrip contextMenu = new ContextMenuStrip();

                foreach (var country in countriesData)
                {
                    ToolStripMenuItem item = new ToolStripMenuItem(country.Name);
                    item.Click += (s, ev) => ShowCountryInfo(country);
                    contextMenu.Items.Add(item);
                }

                contextMenu.Items.Add(new ToolStripSeparator());

                ToolStripMenuItem notesItem = new ToolStripMenuItem("Заметки по стране");
                notesItem.Click += (s, ev) => ShowNotesPanel();
                contextMenu.Items.Add(notesItem);

                contextMenu.Show(mapPanel, e.Location);
            }
        }

        private void ShowNotesPanel()
        {
            if (string.IsNullOrEmpty(currentCountry))
            {
                MessageBox.Show("Сначала выберите страну из контекстного меню (правой кнопкой мыши на карте)",
                    "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            infoPanel.Visible = false;
            notesPanel.Visible = true;
            LoadNotes();
            ClearNoteFields();
        }

        private void ShowCountryInfo(CountryData country)
        {
            currentCountry = country.Name;
            countryNameLbl.Text = $"Название: {country.Name}";
            capitalLbl.Text = $"Столица: {country.Capital}";
            languageLbl.Text = $"Язык: {country.Language}";
            currencyLbl.Text = $"Валюта: {country.Currency}";
            citiesLbl.Text = $"Города: {country.Cities}";

            infoPanel.Visible = true;
            notesPanel.Visible = false;

            // Загружаем заметки для выбранной страны
            LoadNotes();

            // Обновляем комбобокс
            if (countryCombo.Items.Contains(currentCountry))
                countryCombo.SelectedItem = currentCountry;
        }

        private void LoadCountriesData()
        {
            countriesData = GetCountriesData();
            LoadCountries();
        }

        private List<CountryData> GetCountriesData()
        {
            return new List<CountryData>
            {
                new CountryData
                {
                    Name = "Россия",
                    Capital = "Москва",
                    Language = "Русский",
                    Currency = "Рубль (RUB)",
                    Cities = "Москва, Санкт-Петербург, Новосибирск, Екатеринбург, Казань",
                    Color = Color.FromArgb(255, 150, 150)
                },
                new CountryData
                {
                    Name = "Канада",
                    Capital = "Оттава",
                    Language = "Английский, Французский",
                    Currency = "Доллар (CAD)",
                    Cities = "Торонто, Ванкувер, Монреаль, Калгари, Эдмонтон",
                    Color = Color.FromArgb(255, 99, 71)
                },
                new CountryData
                {
                    Name = "США",
                    Capital = "Вашингтон",
                    Language = "Английский",
                    Currency = "Доллар (USD)",
                    Cities = "Нью-Йорк, Лос-Анджелес, Чикаго, Хьюстон, Финикс",
                    Color = Color.FromArgb(139, 69, 19)
                },
                new CountryData
                {
                    Name = "Бразилия",
                    Capital = "Бразилиа",
                    Language = "Португальский",
                    Currency = "Реал (BRL)",
                    Cities = "Сан-Паулу, Рио-де-Жанейро, Сальвадор, Форталеза, Белу-Оризонти",
                    Color = Color.FromArgb(50, 205, 50)
                },
                new CountryData
                {
                    Name = "Аргентина",
                    Capital = "Буэнос-Айрес",
                    Language = "Испанский",
                    Currency = "Песо (ARS)",
                    Cities = "Буэнос-Айрес, Кордова, Росарио, Мендоса, Ла-Плата",
                    Color = Color.FromArgb(135, 206, 235)
                },
                new CountryData
                {
                    Name = "Великобритания",
                    Capital = "Лондон",
                    Language = "Английский",
                    Currency = "Фунт стерлингов (GBP)",
                    Cities = "Лондон, Бирмингем, Манчестер, Ливерпуль, Эдинбург",
                    Color = Color.FromArgb(255, 20, 147)
                },
                new CountryData
                {
                    Name = "Франция",
                    Capital = "Париж",
                    Language = "Французский",
                    Currency = "Евро (EUR)",
                    Cities = "Париж, Марсель, Лион, Тулуза, Ницца",
                    Color = Color.FromArgb(75, 0, 130)
                },
                new CountryData
                {
                    Name = "Германия",
                    Capital = "Берлин",
                    Language = "Немецкий",
                    Currency = "Евро (EUR)",
                    Cities = "Берлин, Гамбург, Мюнхен, Кёльн, Франкфурт",
                    Color = Color.FromArgb(255, 215, 0)
                },
                new CountryData
                {
                    Name = "Италия",
                    Capital = "Рим",
                    Language = "Итальянский",
                    Currency = "Евро (EUR)",
                    Cities = "Рим, Милан, Неаполь, Турин, Палермо",
                    Color = Color.FromArgb(255, 69, 0)
                },
                new CountryData
                {
                    Name = "Китай",
                    Capital = "Пекин",
                    Language = "Китайский",
                    Currency = "Юань (CNY)",
                    Cities = "Пекин, Шанхай, Гуанчжоу, Шэньчжэнь, Тяньцзинь",
                    Color = Color.FromArgb(220, 20, 60)
                },
                new CountryData
                {
                    Name = "Индия",
                    Capital = "Нью-Дели",
                    Language = "Хинди, Английский",
                    Currency = "Рупия (INR)",
                    Cities = "Мумбаи, Дели, Бангалор, Хайдарабад, Ахмедабад",
                    Color = Color.FromArgb(255, 140, 0)
                },
                new CountryData
                {
                    Name = "Япония",
                    Capital = "Токио",
                    Language = "Японский",
                    Currency = "Иена (JPY)",
                    Cities = "Токио, Иокогама, Осака, Нагоя, Саппоро",
                    Color = Color.FromArgb(255, 105, 180)
                },
                new CountryData
                {
                    Name = "Австралия",
                    Capital = "Канберра",
                    Language = "Английский",
                    Currency = "Доллар (AUD)",
                    Cities = "Сидней, Мельбурн, Брисбен, Перт, Аделаида",
                    Color = Color.FromArgb(218, 165, 32)
                },
                new CountryData
                {
                    Name = "ЮАР",
                    Capital = "Претория",
                    Language = "Африкаанс, Английский",
                    Currency = "Рэнд (ZAR)",
                    Cities = "Йоханнесбург, Кейптаун, Дурбан, Претория, Порт-Элизабет",
                    Color = Color.FromArgb(255, 165, 0)
                },
                new CountryData
                {
                    Name = "Египет",
                    Capital = "Каир",
                    Language = "Арабский",
                    Currency = "Фунт (EGP)",
                    Cities = "Каир, Александрия, Гиза, Луксор, Асуан",
                    Color = Color.FromArgb(210, 180, 140)
                }
            };
        }

        private void LoadCountries()
        {
            countryCombo.Items.Clear();
            foreach (var country in countriesData)
            {
                countryCombo.Items.Add(country.Name);
            }
        }

        private void CountryCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (countryCombo.SelectedItem != null)
            {
                currentCountry = countryCombo.SelectedItem.ToString();
                var country = countriesData.FirstOrDefault(c => c.Name == currentCountry);
                if (country != null)
                {
                    countryNameLbl.Text = $"Название: {country.Name}";
                    capitalLbl.Text = $"Столица: {country.Capital}";
                    languageLbl.Text = $"Язык: {country.Language}";
                    currencyLbl.Text = $"Валюта: {country.Currency}";
                    citiesLbl.Text = $"Города: {country.Cities}";
                }
                LoadNotes();
            }
        }

        private void NotesGrid_SelectionChanged(object sender, EventArgs e)
        {
            if (notesGrid.SelectedRows.Count > 0)
            {
                var row = notesGrid.SelectedRows[0];
                noteTitleBox.Text = row.Cells["Title"].Value?.ToString();
                noteTextBox.Text = row.Cells["Content"].Value?.ToString();

                string imagePath = row.Cells["ImagePath"].Value?.ToString();
                if (!string.IsNullOrEmpty(imagePath) && File.Exists(imagePath))
                {
                    notePictureBox.Image = Image.FromFile(imagePath);
                }
                else
                {
                    notePictureBox.Image = null;
                }
            }
        }

        private void UploadImageBtn_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.gif";
                ofd.Title = "Выберите изображение";

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    notePictureBox.Image = Image.FromFile(ofd.FileName);
                }
            }
        }

        private void ClearNoteFields()
        {
            noteTitleBox.Text = "";
            noteTextBox.Text = "";
            notePictureBox.Image = null;
        }

        private void CreateDatabase()
        {
            using (var connection = new SQLiteConnection($"Data Source={dbPath}"))
            {
                connection.Open();
                string createTableQuery = @"
                    CREATE TABLE IF NOT EXISTS Notes (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Country TEXT NOT NULL,
                        Title TEXT NOT NULL,
                        Content TEXT,
                        ImagePath TEXT,
                        CreatedDate TEXT
                    )";

                using (var command = new SQLiteCommand(createTableQuery, connection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }

        private void LoadNotes()
        {
            if (string.IsNullOrEmpty(currentCountry))
            {
                notesGrid.DataSource = null;
                return;
            }

            using (var connection = new SQLiteConnection($"Data Source={dbPath}"))
            {
                connection.Open();
                string query = "SELECT Id, Title, Content, ImagePath, CreatedDate FROM Notes WHERE Country = @Country ORDER BY CreatedDate DESC";

                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Country", currentCountry);
                    using (var reader = command.ExecuteReader())
                    {
                        DataTable dt = new DataTable();
                        dt.Load(reader);
                        notesGrid.DataSource = dt;

                        if (notesGrid.Columns.Count > 0)
                        {
                            notesGrid.Columns["Id"].Visible = false;
                            notesGrid.Columns["Title"].HeaderText = "Название";
                            notesGrid.Columns["Content"].HeaderText = "Содержание";
                            notesGrid.Columns["ImagePath"].HeaderText = "Фото";
                            notesGrid.Columns["CreatedDate"].HeaderText = "Дата";
                        }
                    }
                }
            }
        }

        private void AddNoteBtn_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(currentCountry))
            {
                MessageBox.Show("Пожалуйста, выберите страну", "Предупреждение",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrEmpty(noteTitleBox.Text))
            {
                MessageBox.Show("Введите название заметки", "Предупреждение",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string imagePath = null;
            if (notePictureBox.Image != null)
            {
                string notesDir = Path.Combine(Application.StartupPath, "NotesImages");
                if (!Directory.Exists(notesDir))
                    Directory.CreateDirectory(notesDir);

                imagePath = Path.Combine(notesDir, $"{Guid.NewGuid()}.jpg");
                notePictureBox.Image.Save(imagePath, System.Drawing.Imaging.ImageFormat.Jpeg);
            }

            using (var connection = new SQLiteConnection($"Data Source={dbPath}"))
            {
                connection.Open();
                string query = @"INSERT INTO Notes (Country, Title, Content, ImagePath, CreatedDate) 
                               VALUES (@Country, @Title, @Content, @ImagePath, @CreatedDate)";

                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Country", currentCountry);
                    command.Parameters.AddWithValue("@Title", noteTitleBox.Text);
                    command.Parameters.AddWithValue("@Content", noteTextBox.Text);
                    command.Parameters.AddWithValue("@ImagePath", imagePath ?? "");
                    command.Parameters.AddWithValue("@CreatedDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                    command.ExecuteNonQuery();
                }
            }

            LoadNotes();
            ClearNoteFields();
            MessageBox.Show("Заметка добавлена!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void DeleteNoteBtn_Click(object sender, EventArgs e)
        {
            if (notesGrid.SelectedRows.Count == 0)
            {
                MessageBox.Show("Выберите заметку для удаления", "Предупреждение",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (MessageBox.Show("Вы уверены, что хотите удалить эту заметку?", "Подтверждение",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                int id = Convert.ToInt32(notesGrid.SelectedRows[0].Cells["Id"].Value);

                string imagePath = notesGrid.SelectedRows[0].Cells["ImagePath"].Value?.ToString();
                if (!string.IsNullOrEmpty(imagePath) && File.Exists(imagePath))
                {
                    File.Delete(imagePath);
                }

                using (var connection = new SQLiteConnection($"Data Source={dbPath}"))
                {
                    connection.Open();
                    string query = "DELETE FROM Notes WHERE Id = @Id";

                    using (var command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Id", id);
                        command.ExecuteNonQuery();
                    }
                }

                LoadNotes();
                ClearNoteFields();
                MessageBox.Show("Заметка удалена!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }

    public class CountryData
    {
        public string Name { get; set; }
        public string Capital { get; set; }
        public string Language { get; set; }
        public string Currency { get; set; }
        public string Cities { get; set; }
        public Color Color { get; set; }
    }
}