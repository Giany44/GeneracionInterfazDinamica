using System;
using System.Drawing;
using System.Windows.Forms;
using System.Xml.Linq;
using System.Linq;

namespace GeneracionInterfazDinamica
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            // InitializeComponent() es el único método que debe existir en el Designer.
            InitializeComponent();

            this.Load += new EventHandler(Form1_Load);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Limpieza de interfaz y ajuste de tamaño
            this.Controls.Clear();
            this.MainMenuStrip = null;
            this.Size = new Size(640, 400);

            CargarInterfazDesdeXML("Interfaz.xml");
        }

        private void CargarInterfazDesdeXML(string xmlPath)
        {
            try
            {
                XDocument xmlDoc = XDocument.Load(xmlPath);

                // 1. GENERAR MENÚS
                var menuStripXML = xmlDoc.Element("Formulario")?.Element("MenuStrip");
                if (menuStripXML != null)
                {
                    MenuStrip mainMenuStrip = new MenuStrip();
                    GenerarMenuRecursivo(mainMenuStrip.Items, menuStripXML.Elements("Menu"));

                    this.Controls.Add(mainMenuStrip);
                    this.MainMenuStrip = mainMenuStrip;
                }

                // 2. GENERAR CONTROLES PRINCIPALES Y ANIDADOS
                var controlesXML = xmlDoc.Element("Formulario")?.Element("Controles")?.Elements("Control");
                GenerarControlesRecursivo(this, controlesXML);
            }
            catch (System.IO.FileNotFoundException)
            {
                MessageBox.Show($"Error: El archivo '{xmlPath}' no fue encontrado. Verifique la propiedad 'Copiar en el directorio de salida'.", "Error de Archivo", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error general al cargar la interfaz: {ex.Message}", "Error General", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // --- LÓGICA DE GENERACIÓN DE MENÚS (RECURSIVA) ---
        private void GenerarMenuRecursivo(ToolStripItemCollection items, System.Collections.Generic.IEnumerable<XElement> menusXML)
        {
            if (menusXML == null) return;

            foreach (var menuElement in menusXML)
            {
                string tipo = menuElement.Attribute("Tipo")?.Value;

                if (tipo == "ToolStripMenuItem")
                {
                    string texto = menuElement.Attribute("Texto")?.Value;
                    string handler = menuElement.Attribute("ClickHandler")?.Value;

                    ToolStripMenuItem nuevoItem = new ToolStripMenuItem(texto);

                    if (handler == "MenuItem_Click")
                    {
                        nuevoItem.Click += MenuItem_Click;
                    }

                    var subMenusXML = menuElement.Elements("Menu");
                    if (subMenusXML.Any())
                    {
                        GenerarMenuRecursivo(nuevoItem.DropDownItems, subMenusXML);
                    }

                    items.Add(nuevoItem);
                }
                else if (tipo == "ToolStripSeparator")
                {
                    items.Add(new ToolStripSeparator());
                }
            }
        }

        // --- LÓGICA DE GENERACIÓN DE CONTROLES (RECURSIVA) ---
        private void GenerarControlesRecursivo(Control contenedorPadre, System.Collections.Generic.IEnumerable<XElement> controlesXML)
        {
            if (controlesXML == null) return;

            foreach (var controlElement in controlesXML)
            {
                try
                {
                    string tipo = controlElement.Attribute("Tipo")?.Value;
                    string nombre = controlElement.Attribute("Nombre")?.Value;
                    string texto = controlElement.Attribute("Texto")?.Value;

                    // Parseo de propiedades de diseño
                    int x = int.Parse(controlElement.Attribute("LocationX")?.Value);
                    int y = int.Parse(controlElement.Attribute("LocationY")?.Value);
                    int w = int.Parse(controlElement.Attribute("Width")?.Value);
                    int h = int.Parse(controlElement.Attribute("Height")?.Value);

                    Control nuevoControl = null;

                    // --- CREACIÓN DE CONTROLES ---
                    if (tipo == "Label")
                    {
                        nuevoControl = new Label { AutoSize = false, TextAlign = ContentAlignment.MiddleLeft };
                        // Asignar propiedades de fuente para la etiqueta principal
                        string fontWeight = controlElement.Attribute("FontWeight")?.Value;
                        string fontSize = controlElement.Attribute("FontSize")?.Value;
                        if (fontWeight == "Bold" && int.TryParse(fontSize, out int size))
                        {
                            nuevoControl.Font = new Font(nuevoControl.Font.FontFamily, size, FontStyle.Bold);
                        }
                    }
                    else if (tipo == "Button")
                    {
                        Button btn = new Button();
                        btn.Click += Boton_Click;
                        nuevoControl = btn;
                    }
                    else if (tipo == "Panel")
                    {
                        Panel panel = new Panel();
                        string borderStyle = controlElement.Attribute("BorderStyle")?.Value;
                        if (borderStyle == "FixedSingle")
                        {
                            panel.BorderStyle = BorderStyle.FixedSingle;
                        }

                        // Llamada recursiva para procesar los controles hijos del Panel
                        var controlesHijosXML = controlElement.Element("Controles")?.Elements("Control");
                        GenerarControlesRecursivo(panel, controlesHijosXML);
                        nuevoControl = panel;
                    }
                    else if (tipo == "DataGridView") // SOPORTE PARA TABLA
                    {
                        DataGridView dgv = new DataGridView();
                        dgv.AutoGenerateColumns = false;
                        dgv.AllowUserToAddRows = false;
                        dgv.RowHeadersVisible = false;

                        // 1. Definir Columnas
                        var columnasXML = controlElement.Elements("Columna");
                        foreach (var col in columnasXML)
                        {
                            string colNombre = col.Attribute("Nombre")?.Value;
                            int colWidth = int.Parse(col.Attribute("Width")?.Value);
                            string colTipo = col.Attribute("Tipo")?.Value;
                            bool readOnly = bool.Parse(col.Attribute("ReadOnly")?.Value ?? "False");

                            DataGridViewColumn newCol = null;

                            if (colTipo == "TextBox")
                            {
                                newCol = new DataGridViewTextBoxColumn();
                            }
                            else if (colTipo == "CheckBox")
                            {
                                newCol = new DataGridViewCheckBoxColumn();
                            }

                            if (newCol != null)
                            {
                                newCol.HeaderText = colNombre;
                                newCol.Name = colNombre;
                                newCol.DataPropertyName = col.Attribute("DataField")?.Value;
                                newCol.Width = colWidth;
                                newCol.ReadOnly = readOnly;
                                dgv.Columns.Add(newCol);
                            }
                        }

                        // 2. Añadir Datos Iniciales (Gestión de Proyectos)
                        // Formato de columnas: ID, Proyecto, Tarea, Estado, Prioridad (bool)
                        dgv.Rows.Add(101, "App Móvil", "Diseñar UI/UX", "En Progreso", true);
                        dgv.Rows.Add(102, "App Móvil", "Integración API", "Pendiente", true);
                        dgv.Rows.Add(201, "Web Cliente", "Revisar Contenido", "Completada", false);
                        dgv.Rows.Add(301, "Marketing", "Crear Campaña Q4", "En Progreso", true);
                        dgv.Rows.Add(302, "Marketing", "Reporte Mensual", "Pendiente", false);

                        nuevoControl = dgv;
                    }

                    // --- ASIGNACIÓN DE PROPIEDADES COMUNES (Colores, Nombre, Geometría) ---
                    if (nuevoControl != null)
                    {
                        nuevoControl.Name = nombre;
                        nuevoControl.Text = texto;
                        nuevoControl.Location = new Point(x, y);
                        nuevoControl.Size = new Size(w, h);

                        // Asignación de colores
                        string backColorAttr = controlElement.Attribute("BackColor")?.Value;
                        if (!string.IsNullOrEmpty(backColorAttr))
                        {
                            nuevoControl.BackColor = Color.FromName(backColorAttr);
                        }
                        string foreColorAttr = controlElement.Attribute("ForeColor")?.Value;
                        if (!string.IsNullOrEmpty(foreColorAttr))
                        {
                            nuevoControl.ForeColor = Color.FromName(foreColorAttr);
                        }

                        contenedorPadre.Controls.Add(nuevoControl);
                    }
                }
                catch (FormatException ex)
                {
                    MessageBox.Show($"Error de formato en el control '{controlElement.Attribute("Nombre")?.Value}'. Asegúrese de que todos los valores numéricos son enteros válidos. Detalle: {ex.Message}", "Error de Parseo", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al procesar control '{controlElement.Attribute("Nombre")?.Value}'. Detalle: {ex.Message}", "Error de Generación", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        // --- MANEJADORES DE EVENTOS ---
        private void Boton_Click(object sender, EventArgs e)
        {
            if (sender is Button clickedButton)
            {
                MessageBox.Show($"¡Has hecho clic en el botón '{clickedButton.Text}'!", "Funcionalidad Implementada", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void MenuItem_Click(object sender, EventArgs e)
        {
            if (sender is ToolStripMenuItem clickedItem)
            {
                if (clickedItem.Text == "Salir")
                {
                    Application.Exit();
                }
                else
                {
                    MessageBox.Show($"¡Has seleccionado la opción de menú: {clickedItem.Text}!", "Menú Dinámico", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }
    }
}