import os
import subprocess
import tkinter as tk
from tkinter import filedialog, messagebox
from tkinter.ttk import Combobox
import re

class RomFileGeneratorApp:
    def __init__(self, root):
        self.root = root
        self.root.title("WiguX - TeamGT's EmuVR Capture Core Companion 1.5")

        print("Initializing RomFileGeneratorApp...")  # Debug print
        self.system_commands = self.load_system_commands("systems.dat")
        print("System commands loaded successfully.")  # Debug print
        for system, values in self.system_commands.items():
            print(f"System: {system}, Command: {values[0]}, Extensions: {values[1]}, EXE: {values[2]}, Short Path: {values[3]}, game_system: {values[4]}")  # Debug print

        # Set default values
        self.default_output_path = "../../Games/System Name"
        print(f"Default output path set to {self.default_output_path}")  # Debug print

        # Emulator selection
        self.emulator_label = tk.Label(root, text="Select Emulator (.exe):")
        self.emulator_label.grid(row=0, column=0, padx=10, pady=10)
        self.emulator_path = tk.Entry(root, width=50)
        self.emulator_path.grid(row=0, column=1, padx=10, pady=10)
        self.emulator_button = tk.Button(root, text="Browse", command=self.browse_emulator)
        self.emulator_button.grid(row=0, column=2, padx=10, pady=10)

        # Dropdown for system selection
        self.system_label = tk.Label(root, text="Select System:")
        self.system_label.grid(row=1, column=0, padx=10, pady=10)
        self.system_dropdown = Combobox(root, values=["Select System"] + list(self.system_commands.keys()), state="readonly")
        self.system_dropdown.bind("<<ComboboxSelected>>", self.update_system_settings)
        self.system_dropdown.grid(row=1, column=1, padx=10, pady=10)

        # Additional command-line commands entry
        self.commands_label = tk.Label(root, text="Additional Command-Line Commands:")
        self.commands_label.grid(row=2, column=0, padx=10, pady=10)
        self.commands_entry = tk.Entry(root, width=50)
        self.commands_entry.grid(row=2, column=1, padx=10, pady=10)
        
        # Extensions entry
        self.extensions_label = tk.Label(root, text="File Extensions (e.g., .zip, .chd):")
        self.extensions_label.grid(row=3, column=0, padx=10, pady=10)
        self.extensions_entry = tk.Entry(root, width=50)
        self.extensions_entry.grid(row=3, column=1, padx=10, pady=10)
        
        # Gamesystem entry
        self.game_system = tk.StringVar() 
        self.game_system_label = tk.Label(root, text="EMUVR System Name:")  
        self.game_system_label.grid(row=5, column=0, padx=10, pady=10)
        self.game_system_entry = tk.Entry(root, textvariable=self.game_system, width=50)
        self.game_system_entry.grid(row=5, column=1, padx=10, pady=10)

        # Input ROMs folder selection
        self.input_label = tk.Label(root, text="Select Input ROMs Folder:")
        self.input_label.grid(row=6, column=0, padx=10, pady=10)
        self.input_path = tk.Entry(root, width=50)
        self.input_path.grid(row=6, column=1, padx=10, pady=10)
        self.input_button = tk.Button(root, text="Browse", command=self.browse_input_folder)
        self.input_button.grid(row=6, column=2, padx=10, pady=10)
        
        # Output folder selection
        self.output_label = tk.Label(root, text="Select Output Folder:")
        self.output_label.grid(row=7, column=0, padx=10, pady=10)
        self.output_path = tk.Entry(root, width=50)
        self.output_path.grid(row=7, column=1, padx=10, pady=10)
        self.output_path.insert(0, self.default_output_path)
        self.output_button = tk.Button(root, text="Browse", command=self.browse_output_folder)
        self.output_button.grid(row=7, column=2, padx=10, pady=10)

        # Checkbox for short path option
        self.short_path_var = tk.BooleanVar()
        self.short_path_checkbox = tk.Checkbutton(root, text="Use short ROM path in .bat files", variable=self.short_path_var)
        self.short_path_checkbox.grid(row=8, column=0, columnspan=2, padx=10, pady=10)

        # Informational message and Generate Button
        self.info_label = tk.Label(root, text="Create .win and .bat files in the output directory.")
        self.info_label.grid(row=9, column=0, padx=10, pady=10, sticky='w')
        self.generate_button = tk.Button(root, text="Generate Capture Core Files", command=self.generate_files)
        self.generate_button.grid(row=11, column=1, padx=10, pady=10, sticky='e')

        # Buttons for PS3, Vita, Xbox 360, XCloud, and Teknoparrot
        self.ps3_button = tk.Button(root, text="Launch PS3 Companion for RPCS3", command=self.launch_ps3)
        self.ps3_button.grid(row=0, column=3, padx=0, pady=10, sticky='ew')
        self.vita_button = tk.Button(root, text="Launch Vita Companion for Vita3K", command=self.launch_vita)
        self.vita_button.grid(row=1, column=3, padx=0, pady=10, sticky='ew')
        self.xbox360_button = tk.Button(root, text="Launch Xbox 360 Companion for Xenia", command=self.launch_xbox360)
        self.xbox360_button.grid(row=2, column=3, padx=0, pady=10, sticky='ew')
        self.xcloud_button = tk.Button(root, text="Launch Xbox One/Series Companion for XCloud", command=self.launch_xcloud)
        self.xcloud_button.grid(row=3, column=3, padx=0, pady=10, sticky='ew')
        self.tp_button = tk.Button(root, text="Launch Teknoparrot Companion", command=self.launch_tp)
        self.tp_button.grid(row=4, column=3, padx=0, pady=10, sticky='ew')
        self.exe_button = tk.Button(root, text="Launch EXE Companion", command=self.launch_exe)
        self.exe_button.grid(row=5, column=3, padx=0, pady=10, sticky='ew')
        self.exe_button = tk.Button(root, text="Launch M.U.G.E.N./OpenBor Companion", command=self.launch_openbor)
        self.exe_button.grid(row=6, column=3, padx=0, pady=10, sticky='ew')
        self.hikaru_button = tk.Button(root, text="Launch Sega Hikaru Companion", command=self.launch_hikaru)
        self.hikaru_button.grid(row=7, column=3, padx=0, pady=10, sticky='ew')
        self.segam2_button = tk.Button(root, text="Launch Sega Model 2 Companion", command=self.launch_segam2)
        self.segam2_button.grid(row=8, column=3, padx=0, pady=10, sticky='ew')
        self.segam3_button = tk.Button(root, text="Launch Sega Model 3 Companion", command=self.launch_segam3)
        self.segam3_button.grid(row=9, column=3, padx=0, pady=10, sticky='ew')
        self.pinball_button = tk.Button(root, text="Launch Pinball FX/M Companion", command=self.launch_pinball)
        self.pinball_button.grid(row=10, column=3, padx=0, pady=10, sticky='ew')

        root.grid_columnconfigure(0, weight=1)
        root.grid_columnconfigure(1, weight=1)
        root.grid_columnconfigure(2, weight=1)
        root.grid_columnconfigure(3, weight=1)
    
    def update_system_settings(self, event):
        selected_system = self.system_dropdown.get()
        print(f"Selected system: {selected_system}")  # Debug print
        if selected_system != "Select System":
            command, extensions, recommended_exe, short_path_default, game_system = self.system_commands[selected_system]
            print(f"Updating settings for {selected_system}: command={command}, extensions={extensions}, exe={recommended_exe}, short_path={short_path_default}, game_system={game_system}")  # Debug print

            # Update the command line entry box
            self.commands_entry.delete(0, tk.END)
            self.commands_entry.insert(0, command)
            print(f"Command line entry updated: {command}")  # Debug print

            # Update the extensions entry box
            self.extensions_entry.delete(0, tk.END)
            self.extensions_entry.insert(0, ", ".join(extensions) if extensions else "")
            print(f"Extensions entry updated: {extensions}")  # Debug print

            # Update the emulator path entry box with the recommended exe
            self.emulator_path.delete(0, tk.END)
            self.emulator_path.insert(0, recommended_exe)
            print(f"Emulator path entry updated: {recommended_exe}")  # Debug print

            # Update the checkbox state
            self.short_path_var.set(short_path_default)
            print(f"Checkbox state updated: {short_path_default}")  # Debug print
            
            # Update the gamesystem
            self.game_system_entry.delete(0, tk.END)
            self.game_system_entry.insert(0, game_system)
            print(f"EmuVR System Name updated: {game_system}")  # Debug print
            
            # Update the default output path with the game_system
            self.default_output_path = f"../../Games/{game_system}"
            print(f"Default output path updated: {self.default_output_path}")  # Debug print

            # Update the output path entry field with the new path
            self.output_path.delete(0, tk.END)  # Clear the previous value
            self.output_path.insert(0, self.default_output_path)  # Insert the new output path
            print(f"Output path entry updated: {self.default_output_path}")  # Debug print

    def load_system_commands(self, file_path):
        system_commands = {}
        print(f"Loading system commands from {file_path}...")  # Debug print
        try:
            with open(file_path, 'r', encoding='utf-8') as csvfile:
                rows = csvfile.readlines()
                print(f"Read {len(rows)} lines from {file_path}.")  # Debug print
                for i, row in enumerate(rows):
                    print(f"Processing line {i+1}: {row.strip()}")  # Debug print
                    data = row.strip().split('\t')
                    print(f"Split data: {data}")  # Debug print
                    if len(data) == 6:
                        system = data[0].strip()
                        command = data[1].strip()
                        extensions = [ext.strip() for ext in data[2].split(',')] if data[2].strip() else []
                        recommended_exe = data[3].strip()
                        short_path_default = bool(int(data[4].strip()))
                        system_commands[system] = (command, extensions, recommended_exe, short_path_default)
                        game_system = data[5].strip()
                        system_commands[system] = (command, extensions, recommended_exe, short_path_default, game_system)
                        print(f"Loaded system: {system} with command: {command}, extensions: {extensions}, exe: {recommended_exe}, short_path: {short_path_default}, gamesystem: {game_system}")  # Debug print
                    else:
                        print(f"Skipping invalid line {i+1}: {row.strip()}")  # Debug print
            print("All systems loaded successfully.")
        except Exception as e:
            print(f"Failed to load system commands: {e}")  # Debug print
            messagebox.showerror("Error", f"Failed to load system commands: {e}")
        return system_commands

    def sanitize_filename(self, filename):
        print(f"Sanitizing filename: {filename}")  # Debug print
        # Replace invalid characters with " -", remove multiple spaces, and strip leading/trailing spaces
        sanitized = re.sub(r'[<>:"/\\|?*]', ' -', filename)
        sanitized = re.sub(r'\s+', ' ', sanitized).strip()
        print(f"Sanitized filename: {sanitized}")  # Debug print
        return sanitized

    def browse_emulator(self):
        file_path = filedialog.askopenfilename(filetypes=[("Executable Files", "*.exe")])
        print(f"Selected emulator file: {file_path}")  # Debug print
        if file_path:
            self.emulator_path.delete(0, tk.END)
            self.emulator_path.insert(0, file_path)
            print(f"Emulator path updated: {file_path}")  # Debug print
            
    def browse_output_folder(self):
        folder_path = filedialog.askdirectory()
        print(f"Selected output folder: {folder_path}")  # Debug print
        if folder_path:
            self.output_path.delete(0, tk.END)
            self.output_path.insert(0, folder_path)
            print(f"Output path updated: {folder_path}")  # Debug print

    def browse_input_folder(self):
        folder_path = filedialog.askdirectory()
        print(f"Selected input folder: {folder_path}")  # Debug print
        if folder_path:
            self.input_path.delete(0, tk.END)
            self.input_path.insert(0, folder_path)
            print(f"Input path updated: {folder_path}")  # Debug print

    def generate_files(self):
        print("Generating capture core files...")  # Debug print
        # Get the selected system from the dropdown
        selected_system = self.system_dropdown.get()
        print(f"Selected system for file generation: {selected_system}")  # Debug print

        # Get the selected emulator and additional commands
        emulator = self.emulator_path.get()
        additional_commands = self.commands_entry.get()
        print(f"Using emulator: {emulator}")  # Debug print
        print(f"Additional commands: {additional_commands}")  # Debug print

        # Get the input and output folder paths
        input_folder = self.input_path.get()
        output_folder = self.output_path.get()
        print(f"Input folder: {input_folder}")  # Debug print
        print(f"Output folder: {output_folder}")  # Debug print

        # Get the checkbox state
        use_short_path = self.short_path_var.get()
        print(f"Use short path: {use_short_path}")  # Debug print
        
        # Get Game System
        game_system = self.game_system.get()
        print(f"Game System: {game_system}")  # Debug print

        # Get the user-provided extensions from the entry box
        extensions = self.extensions_entry.get().split(',')
        extensions = [ext.strip() for ext in extensions]
        print(f"Extensions for file generation: {extensions}")  # Debug print
        
        # Generate .win files for ROMs in the input folder
        for root, _, files in os.walk(input_folder):
            print(f"Processing folder: {root}")  # Debug print
            for file in files:
                print(f"Processing file: {file}")  # Debug print
                if any(file.endswith(ext) for ext in extensions):
                    rom_name, _ = os.path.splitext(file)
                    sanitized_rom_name = self.sanitize_filename(rom_name)
                    win_file_path = os.path.join(output_folder, f"{sanitized_rom_name}.win")
                    bat_file_path = os.path.join(output_folder, f"{sanitized_rom_name}.bat")
                    
                # Part 1: Clean the emulator filename
                emulator_filename = os.path.basename(emulator).strip(' "')

                # Check if the filename starts with " &&" or contains it, and remove it
                if emulator_filename.startswith("&&"):
                    emulator_filename = emulator_filename[3:].strip()

                # Part 1: Clean the emulator filename
                emulator_filename = os.path.basename(emulator).strip(' "')

                # Check if the filename starts with " &&" or contains it, and remove it
                if emulator_filename.startswith("&&"):
                    emulator_filename = emulator_filename[3:].strip()

                # Ensure the emulator path uses backslashes
                emulator = emulator.replace('/', '\\')

                # Create .bat file with the full path to the emulator and additional commands
                rom_path = os.path.join(root, file)
                rom_path = rom_path.replace('/', '\\')  # Ensure backslashes in the ROM path
                with open(bat_file_path, 'w', encoding='utf-8') as bat_file:
                    if use_short_path:
                        bat_file.write(f'cd ./Games/Arcade (Capture)\n')
                        bat_file.write(f'cd ../..\n')
                        bat_file.write(f'{emulator} {additional_commands} {file}\n')
                        print(f"Created .bat file: {bat_file_path} with content: {emulator} {additional_commands} {file}")  # Debug print
                    else:
                        bat_file.write(f'cd ./Games/Arcade (Capture)\n')
                        bat_file.write(f'cd ../..\n')
                        bat_file.write(f'{emulator} {additional_commands} "{rom_path}"\n') 
                        print(f"Created .bat file: {bat_file_path} with content: {emulator} {additional_commands} \"{rom_path}\"")  # Debug print

                    # Generate emuvr_core.txt
                    emuvr_core_filepath = os.path.join(output_folder, "emuvr_core.txt")
                    with open(emuvr_core_filepath, 'w') as core_file:
                        core_file.write('media = "{}"\n'.format(game_system))
                        core_file.write('core = "wgc_libretro"\n')
                        core_file.write('noscanlines = "true"\n')
                        core_file.write('aspect_ratio = "auto"\n')

                    # Generate emuvr_override_auto.cfg
                    emuvr_override_filepath = os.path.join(output_folder, "emuvr_override_auto.cfg")
                    with open(emuvr_override_filepath, 'w') as override_file:
                        override_file.write('input_player1_analog_dpad_mode = "0"\n')
                        override_file.write('video_shader = "shaders\\shaders_glsl\\stock.glslp"\n')
                        override_file.write('video_threaded = "false"\n')
                        override_file.write('video_vsync = "true"\n')

        messagebox.showinfo("Success", f"Capture Core files generated successfully.\nMedia Set to {game_system}.")
    
    def launch_ps3(self):
        print("Launching PS3 Companion for RPCS3...")  # Debug print
        subprocess.Popen(['python', '_internal/ps3.py'])

    def launch_vita(self):
        print("Launching Vita Companion for Vita3K...")  # Debug print
        subprocess.Popen(['python', '_internal/vita.py'])

    def launch_xbox360(self):
        print("Launching Xbox 360 Companion for Xenia...")  # Debug print
        subprocess.Popen(['python', '_internal/360.py'])
        
    def launch_xcloud(self):
        print("Launching Xbox One/Series Companion for XCloud...")  # Debug print
        subprocess.Popen(['python', '_internal/xcloud.py'])
        
    def launch_exe(self):
        print("Launching EXE Companion...")  # Debug print
        subprocess.Popen(['python', '_internal/exe.py'])
        
    def launch_openbor(self):
        print("Launching M.U.G.E.N./OpenBor Companion...")  # Debug print
        subprocess.Popen(['python', '_internal/openbor.py'])
        
    def launch_tp(self):
        print("Launching Teknoparrot Companion...")  # Debug print
        subprocess.Popen(['python', '_internal/tp.py'])
        
    def launch_hikaru(self):
        print("Launching Sega Hikaru Companion...")  # Debug print
        subprocess.Popen(['python', '_internal/hikaru.py'])       
 
    def launch_segam2(self):
        print("Launching Sega Model 2 Companion...")  # Debug print
        subprocess.Popen(['python', '_internal/segam2.py'])        
        
    def launch_segam3(self):
        print("Launching Sega Model 3 Companion...")  # Debug print
        subprocess.Popen(['python', '_internal/segam3.py'])   
        
    def launch_pinball(self):
        print("Launching Pinball FX Companion...")  # Debug print
        subprocess.Popen(['python', '_internal/pinball.py'])               
        

if __name__ == "__main__":
    print("Starting application...")  # Debug print
    root = tk.Tk()
    app = RomFileGeneratorApp(root)
    print("Running main loop...")  # Debug print
    root.mainloop()
    print("Application closed.")  # Debug print
