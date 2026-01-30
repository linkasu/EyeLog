using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using EyeLog.Models;
using EyeLog.Server;
using EyeLog.Services;
using EyeLog.State;

namespace EyeLog.Tray
{
    internal class ClientSetupForm : Form
    {
        private readonly ServerOptions options;
        private readonly ClientRegistry registry;
        private readonly LogBuffer logBuffer;
        private readonly Action<string> onClientIdChanged;
        private string clientId;

        private TextBox clientIdBox;
        private NumericUpDown timeoutInput;
        private DataGridView boundsGrid;
        private Label statusLabel;

        public ClientSetupForm(ServerOptions options, ClientRegistry registry, LogBuffer logBuffer, string clientId, Action<string> onClientIdChanged)
        {
            this.options = options;
            this.registry = registry;
            this.logBuffer = logBuffer;
            this.clientId = clientId;
            this.onClientIdChanged = onClientIdChanged;

            Text = "EyeLog Client Setup";
            MinimumSize = new Size(720, 520);
            StartPosition = FormStartPosition.CenterScreen;
            BuildLayout();
            LoadClient();
        }

        private void BuildLayout()
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 6,
                Padding = new Padding(16),
                AutoSize = true
            };

            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var header = new Label
            {
                Text = "Create a client ID, define bounds, and share the ID with your browser client.",
                AutoSize = true,
                Font = new Font(Font.FontFamily, 10, FontStyle.Bold)
            };

            layout.Controls.Add(header, 0, 0);
            layout.Controls.Add(BuildClientPanel(), 0, 1);
            layout.Controls.Add(BuildBoundsPanel(), 0, 2);
            layout.Controls.Add(BuildTimeoutPanel(), 0, 3);
            layout.Controls.Add(BuildButtonsPanel(), 0, 4);

            statusLabel = new Label
            {
                Text = "",
                AutoSize = true,
                ForeColor = Color.DarkGreen
            };
            layout.Controls.Add(statusLabel, 0, 5);

            Controls.Add(layout);
        }

        private Control BuildClientPanel()
        {
            var panel = new GroupBox
            {
                Text = "Client",
                Dock = DockStyle.Fill,
                Padding = new Padding(12)
            };

            var inner = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 2,
                AutoSize = true
            };
            inner.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            inner.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            inner.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            inner.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            inner.Controls.Add(new Label { Text = "Client ID", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);

            clientIdBox = new TextBox
            {
                ReadOnly = true,
                Dock = DockStyle.Fill
            };
            inner.Controls.Add(clientIdBox, 1, 0);

            var copyButton = new Button
            {
                Text = "Copy",
                AutoSize = true
            };
            copyButton.Click += (_, __) => CopyClientId();
            inner.Controls.Add(copyButton, 2, 0);

            var newButton = new Button
            {
                Text = "New",
                AutoSize = true
            };
            newButton.Click += (_, __) => GenerateNewClientId();
            inner.Controls.Add(newButton, 3, 0);

            var urlLabel = new Label
            {
                Text = "Connect using: " + options.Prefix.TrimEnd('/') + "/bounds?clientId=...",
                AutoSize = true,
                ForeColor = Color.DimGray
            };
            inner.Controls.Add(urlLabel, 1, 1);
            inner.SetColumnSpan(urlLabel, 3);

            panel.Controls.Add(inner);
            return panel;
        }

        private Control BuildBoundsPanel()
        {
            var panel = new GroupBox
            {
                Text = "Bounds",
                Dock = DockStyle.Fill,
                Padding = new Padding(12)
            };

            var inner = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2
            };
            inner.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            inner.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            boundsGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = true,
                AllowUserToDeleteRows = true,
                RowHeadersVisible = false
            };

            boundsGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "X" });
            boundsGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Y" });
            boundsGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Width" });
            boundsGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Height" });

            inner.Controls.Add(boundsGrid, 0, 0);

            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true
            };
            var addRow = new Button { Text = "Add row", AutoSize = true };
            addRow.Click += (_, __) => boundsGrid.Rows.Add(0, 0, 100, 100);
            var removeRow = new Button { Text = "Remove selected", AutoSize = true };
            removeRow.Click += (_, __) => RemoveSelectedRows();
            buttons.Controls.Add(addRow);
            buttons.Controls.Add(removeRow);

            inner.Controls.Add(buttons, 0, 1);
            panel.Controls.Add(inner);
            return panel;
        }

        private Control BuildTimeoutPanel()
        {
            var panel = new GroupBox
            {
                Text = "Click timeout",
                Dock = DockStyle.Fill,
                Padding = new Padding(12)
            };

            var inner = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true
            };

            inner.Controls.Add(new Label { Text = "Timeout (ms)", AutoSize = true, Anchor = AnchorStyles.Left });
            timeoutInput = new NumericUpDown
            {
                Minimum = 50,
                Maximum = 10000,
                Increment = 50,
                Width = 120
            };
            inner.Controls.Add(timeoutInput);
            panel.Controls.Add(inner);
            return panel;
        }

        private Control BuildButtonsPanel()
        {
            var panel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                FlowDirection = FlowDirection.RightToLeft
            };

            var closeButton = new Button { Text = "Close", AutoSize = true };
            closeButton.Click += (_, __) => Close();

            var applyButton = new Button { Text = "Apply", AutoSize = true };
            applyButton.Click += (_, __) => ApplyChanges();

            var openLogs = new Button { Text = "Open logs", AutoSize = true };
            openLogs.Click += (_, __) => OpenLogs();

            panel.Controls.Add(closeButton);
            panel.Controls.Add(applyButton);
            panel.Controls.Add(openLogs);
            return panel;
        }

        private void LoadClient()
        {
            clientIdBox.Text = clientId;

            var client = registry.GetOrCreate(clientId);
            var bounds = client.GetBoundsSnapshot();

            boundsGrid.Rows.Clear();
            foreach (var bound in bounds)
            {
                boundsGrid.Rows.Add(bound.X, bound.Y, bound.Width, bound.Height);
            }

            timeoutInput.Value = client.GetClickTimeout();
        }

        private void ApplyChanges()
        {
            var bounds = new List<BoundDto>();
            foreach (DataGridViewRow row in boundsGrid.Rows)
            {
                if (row.IsNewRow)
                {
                    continue;
                }

                if (!TryReadCell(row, 0, out var x) ||
                    !TryReadCell(row, 1, out var y) ||
                    !TryReadCell(row, 2, out var width) ||
                    !TryReadCell(row, 3, out var height))
                {
                    MessageBox.Show("Please fill all bounds with integer values.", "Invalid bounds", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                bounds.Add(new BoundDto { X = x, Y = y, Width = width, Height = height });
            }

            var client = registry.GetOrCreate(clientId);
            client.SetBounds(bounds);
            client.SetClickTimeout((int)timeoutInput.Value);

            logBuffer.Add("ui: bounds set (" + clientId + ", " + bounds.Count + ")");
            statusLabel.Text = "Saved at " + DateTime.Now.ToShortTimeString();
        }

        private void RemoveSelectedRows()
        {
            foreach (DataGridViewRow row in boundsGrid.SelectedRows)
            {
                if (!row.IsNewRow)
                {
                    boundsGrid.Rows.Remove(row);
                }
            }
        }

        private void CopyClientId()
        {
            try
            {
                Clipboard.SetText(clientId);
                statusLabel.Text = "Client ID copied";
            }
            catch
            {
            }
        }

        private void GenerateNewClientId()
        {
            clientId = Guid.NewGuid().ToString("N");
            clientIdBox.Text = clientId;
            onClientIdChanged?.Invoke(clientId);
            statusLabel.Text = "New Client ID generated";
        }

        private void OpenLogs()
        {
            try
            {
                var url = options.Prefix + "logs";
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch
            {
            }
        }

        private static bool TryReadCell(DataGridViewRow row, int index, out int value)
        {
            value = 0;
            var cellValue = row.Cells[index].Value?.ToString();
            return int.TryParse(cellValue, out value);
        }
    }
}
