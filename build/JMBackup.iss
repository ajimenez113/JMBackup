; Instalador de JMBackup (fase 9, hito 2).
;
; Compilar con:
;   iscc build\JMBackup.iss /DMyAppVersion=1.0.0
; o, más cómodo, con build\Build-Installer.ps1, que publica los dos ejecutables,
; toma la versión de Directory.Build.props y llama a ISCC con esa versión y la
; carpeta de publicación correctas.
;
; Requiere las carpetas publicadas en MyPublishDir (por defecto build\publish, la
; misma que usa Build-Installer.ps1 — deliberadamente NO la "publish\" de la raíz que
; usa la instalación manual del README, para no chocar con un servicio de desarrollo
; que ya esté corriendo desde ahí):
;   MyPublishDir\           JMBackup.Api.exe (servicio) autocontenido
;   MyPublishDir\desktop\   JMBackup.Desktop.exe autocontenido

#ifndef MyAppVersion
  #define MyAppVersion "0.0.0-dev"
#endif

#ifndef MyPublishDir
  #define MyPublishDir "publish"
#endif

#define MyAppName "JMBackup"
#define MyAppPublisher "JMBackup"
#define MyServiceName "JMBackup"

[Setup]
AppId={{454DB190-7786-44E6-BF95-0E178152EECD}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\JMBackup
DefaultGroupName=JMBackup
DisableProgramGroupPage=yes
UninstallDisplayIcon={app}\desktop\JMBackup.Desktop.exe
OutputDir=dist
OutputBaseFilename=JMBackup-Setup-{#MyAppVersion}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
; RNF-01: Windows 10 1809 (build 17763) en adelante, x64 únicamente.
MinVersion=10.0.17763
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
; La instalación crea una cuenta de servicio, registra un servicio de Windows,
; abre una regla de firewall y toca HKLM: necesita administrador sí o sí.
PrivilegesRequired=admin
; Firma de código (RNF-06): sin certificado disponible todavía (ver
; docs/adr — "Instalador y firma de código"). Cuando exista, se firma este
; instalador ya compilado con build\Sign-Artifacts.ps1, no acá — SignTool firma el
; .exe de salida de ISCC, Inno Setup no firma sus propios instaladores por sí solo.

[Languages]
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"

[Tasks]
Name: "desktopicon"; Description: "Crear un acceso directo en el escritorio"; GroupDescription: "Accesos directos:"

[Files]
Source: "{#MyPublishDir}\*"; DestDir: "{app}"; Excludes: "desktop\*"; Flags: recursesubdirs ignoreversion
Source: "{#MyPublishDir}\desktop\*"; DestDir: "{app}\desktop"; Flags: recursesubdirs ignoreversion
Source: "Install-JMBackupService.ps1"; DestDir: "{app}\build"; Flags: ignoreversion
Source: "Install-TrustedRootCertificate.ps1"; DestDir: "{app}\build"; Flags: ignoreversion
Source: "Remove-TrustedRootCertificate.ps1"; DestDir: "{app}\build"; Flags: ignoreversion

[Icons]
Name: "{group}\JMBackup"; Filename: "{app}\desktop\JMBackup.Desktop.exe"
Name: "{group}\Desinstalar JMBackup"; Filename: "{uninstallexe}"
Name: "{autodesktop}\JMBackup"; Filename: "{app}\desktop\JMBackup.Desktop.exe"; Tasks: desktopicon

[Run]
; 1. Cuenta de servicio dedicada, derecho "Iniciar sesión como servicio", registro
;    del servicio (arranque automático retrasado), regla de firewall y
;    LongPathsEnabled: todo lo que ya hacía el script de hito 1, ahora invocado
;    automáticamente en vez de a mano.
Filename: "powershell.exe"; \
    Parameters: "-NoProfile -ExecutionPolicy Bypass -File ""{app}\build\Install-JMBackupService.ps1"" -InstallPath ""{app}"""; \
    StatusMsg: "Configurando el servicio de JMBackup..."; \
    Flags: runhidden waituntilterminated

; 2. Certificado de confianza: solo si la persona marcó la casilla en la página de
;    confianza del certificado (ver InitializeWizard más abajo). El servicio ya tiene
;    que estar arrancado (paso 1) para que el .pfx exista.
Filename: "powershell.exe"; \
    Parameters: "-NoProfile -ExecutionPolicy Bypass -File ""{app}\build\Install-TrustedRootCertificate.ps1"""; \
    StatusMsg: "Instalando el certificado de confianza..."; \
    Flags: runhidden waituntilterminated; \
    Check: ShouldTrustCertificate

[UninstallRun]
; El orden importa: primero quitar el certificado de confianza (necesita el script
; que todavía está en {app}\build en este punto), después detener y borrar el
; servicio, después la regla de firewall. Los archivos de {app} recién se borran
; después de que termina esta sección.
Filename: "powershell.exe"; \
    Parameters: "-NoProfile -ExecutionPolicy Bypass -File ""{app}\build\Remove-TrustedRootCertificate.ps1"""; \
    RunOnceId: "JMBackupRemoveTrustedCert"; \
    Flags: runhidden waituntilterminated
Filename: "{sys}\sc.exe"; Parameters: "stop {#MyServiceName}"; \
    RunOnceId: "JMBackupStopService"; Flags: runhidden waituntilterminated
Filename: "{sys}\sc.exe"; Parameters: "delete {#MyServiceName}"; \
    RunOnceId: "JMBackupDeleteService"; Flags: runhidden waituntilterminated
Filename: "powershell.exe"; \
    Parameters: "-NoProfile -Command ""Remove-NetFirewallRule -DisplayName '{#MyServiceName}' -ErrorAction SilentlyContinue"""; \
    RunOnceId: "JMBackupRemoveFirewallRule"; Flags: runhidden waituntilterminated

[Code]
var
  TrustCertPage: TWizardPage;
  TrustCertCheckBox: TNewCheckBox;

procedure InitializeWizard;
var
  ExplanationLabel: TNewStaticText;
begin
  TrustCertPage := CreateCustomPage(wpSelectTasks,
    'Certificado HTTPS de JMBackup', 'Confianza del certificado autofirmado');

  ExplanationLabel := TNewStaticText.Create(TrustCertPage);
  ExplanationLabel.Parent := TrustCertPage.Surface;
  ExplanationLabel.AutoSize := False;
  ExplanationLabel.WordWrap := True;
  ExplanationLabel.Left := 0;
  ExplanationLabel.Top := 0;
  ExplanationLabel.Width := TrustCertPage.SurfaceWidth;
  ExplanationLabel.Height := 180;
  ExplanationLabel.Caption :=
    'JMBackup genera su propio certificado para cifrar la conexión con su interfaz ' +
    'web. Al no estar firmado por una entidad reconocida, el navegador y la ' +
    'aplicación de escritorio van a mostrar una advertencia de conexión no segura la ' +
    'primera vez que se usen — aunque la conexión ya está cifrada de todas formas.' + #13#10#13#10 +
    'Si marcás esta casilla, Windows va a confiar en ese certificado y la ' +
    'advertencia desaparece. Esto significa que el sistema va a aceptar como válida ' +
    'cualquier conexión HTTPS firmada con la clave de este certificado. Esa clave ' +
    'nunca sale de este equipo, así que en la práctica solo la usa JMBackup — pero ' +
    'es una decisión de confianza real, no un trámite. Si preferís evitarla, dejá la ' +
    'casilla sin marcar: vas a poder aceptar la advertencia del navegador como ' +
    'excepción cada vez que haga falta.';

  TrustCertCheckBox := TNewCheckBox.Create(TrustCertPage);
  TrustCertCheckBox.Parent := TrustCertPage.Surface;
  TrustCertCheckBox.Left := 0;
  TrustCertCheckBox.Top := ExplanationLabel.Top + ExplanationLabel.Height + 16;
  TrustCertCheckBox.Width := TrustCertPage.SurfaceWidth;
  TrustCertCheckBox.Caption := 'Confiar en el certificado HTTPS de JMBackup en este equipo';
  TrustCertCheckBox.Checked := False;
end;

function ShouldTrustCertificate: Boolean;
begin
  Result := TrustCertCheckBox.Checked;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  DataDir: String;
  Response: Integer;
begin
  if CurUninstallStep = usPostUninstall then
  begin
    DataDir := ExpandConstant('{commonappdata}\JMBackup');
    if DirExists(DataDir) then
    begin
      Response := MsgBox(
        'JMBackup guardó su base de datos y su configuración en:' + #13#10 +
        DataDir + #13#10#13#10 +
        '¿Querés conservarlos? Si vas a reinstalar JMBackup más adelante, ' +
        'conservarlos te evita configurar todo de nuevo.' + #13#10#13#10 +
        'Elegí "No" solo si querés borrar definitivamente el historial, las ' +
        'tareas guardadas y las credenciales.',
        mbConfirmation, MB_YESNO);
      if Response = IDNO then
      begin
        DelTree(DataDir, True, True, True);
      end;
    end;
  end;
end;
