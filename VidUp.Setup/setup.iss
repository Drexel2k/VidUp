[Setup]
AppName=VidUp
AppId=VidUpDrexelDevelopment
AppVersion=1.13.0
WizardStyle=modern
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
DefaultDirName={autopf}\VidUp
DefaultGroupName=VidUp
UninstallDisplayIcon={app}\VidUp.exe
Compression=lzma2
SolidCompression=yes
OutputBaseFilename=VidUp.Setup.Ver.Release.x64
OutputDir=..\VidUp.Setup\bin\Release\x64\

[Files]
Source: ..\VidUp.UI\bin\Release\x64\net6.0-windows\*; DestDir: "{app}"; Flags: recursesubdirs

[Icons]
Name: "{group}\VidUp"; Filename: "{app}\VidUp.exe"

[Code]
// check for old version ////////////////////////
function GetUninstallString(): String;
var
  UninstallPath: String;
  UninstallString: String;
begin
  UninstallPath := ExpandConstant('Software\Microsoft\Windows\CurrentVersion\Uninstall\{#emit SetupSetting("AppId")}_is1');
  UninstallString := '';
  RegQueryStringValue(HKLM, UninstallPath, 'UninstallString', UninstallString);
  Result := UninstallString;
end;

function IsNet6Installed(): Boolean;
var
  Net6Path: String;
  Net6String: String;
  Net6FirstChar: String;
begin
  Net6Path := 'Software\dotnet\Setup\InstalledVersions\x64\sharedhost';
  Net6String := '';
  Result := false;
  if RegQueryStringValue(HKLM, Net6Path, 'Version', Net6String) then
  begin
    Net6FirstChar := Copy(Net6String, 1, 1)
    if Net6FirstChar = '6' then
    begin
      Result:= true;
    end;
  end
end;


function IsUpgrade(): Boolean;
begin
  Result := (GetUninstallString() <> '');
end;


function UnInstallOldVersion(): Integer;
var
  UninstallString: String;
  ResultCode: Integer;
begin
{ Return Values: }
{ 1 - uninstall string is empty }
{ 2 - error executing the UnInstallString }
{ 3 - successfully executed the UnInstallString }

  { default return value }
  Result := 0;

  { get the uninstall string of the old app }
  UninstallString := GetUninstallString();
  if UninstallString <> '' then
  begin
    UninstallString := RemoveQuotes(UninstallString);
    if Exec(UninstallString, '/SILENT /NORESTART /SUPPRESSMSGBOXES','', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
    begin
      Result := 3
    end else
    begin
      Result := 2;
    end;
  end else
  begin
    Result := 1;
  end;
end;


// add license and privacy dialog /////////////////////////// 
var
  FirstAgreementPage: TOutputMsgMemoWizardPage;
  FirstAgreementPageAcceptedRadio: TRadioButton;
  FirstAgreementPageNotAcceptedRadio: TRadioButton; 
  SecondAgreementPage: TOutputMsgMemoWizardPage;
  SecondAgreementPageAcceptedRadio: TRadioButton;
  SecondAgreementPageNotAcceptedRadio: TRadioButton;   

procedure AgreementPageAccepted(Sender: TObject);
begin
  if (Sender = FirstAgreementPageAcceptedRadio) or (Sender = FirstAgreementPageNotAcceptedRadio) then
  begin
    // update Next button when user (un)accepts the agreement
    WizardForm.NextButton.Enabled := FirstAgreementPageAcceptedRadio.Checked;
  end;

  if (Sender = SecondAgreementPageAcceptedRadio) or (Sender = SecondAgreementPageNotAcceptedRadio) then
  begin
    // update Next button when user (un)accepts the agreement
    WizardForm.NextButton.Enabled := SecondAgreementPageAcceptedRadio.Checked;
  end;
end;

procedure SecondAgreementPageAccepted(Sender: TObject);
begin
  // update Next button when user (un)accepts the agreement
  WizardForm.NextButton.Enabled := SecondAgreementPageAcceptedRadio.Checked;
end;

function CreateRadioButton(Parent: TNewNotebookPage; Top: Integer; Caption: String): TRadioButton;
begin
  Result := TRadioButton.Create(WizardForm);
  Result.Parent := Parent;
  Result.Caption := Caption;
  Result.Left := 0;
  Result.Top := Top;
  Result.Width := ScaleX(417);
  Result.Height := ScaleY(17);
  Result.Anchors := [akLeft, akBottom];
  Result.OnClick := @AgreementPageAccepted;
end;

function CreateAgreementPage(AfterID: Integer; Caption: String; Description: String; RtfFile: String): TOutputMsgMemoWizardPage;
var
  RichText: AnsiString;
begin
  Result :=
    CreateOutputMsgMemoPage(AfterID, Caption, SetupMessage(msgLicenseLabel), Description, '');

  Result.RichEditViewer.Top := ScaleY(50);
  Result.RichEditViewer.Height := ScaleY(135);

  // display file
  ExtractTemporaryFile(RtfFile);
  LoadStringFromFile(ExpandConstant('{tmp}\' + RtfFile), RichText);
  Result.RichEditViewer.UseRichEdit := True;
  Result.RichEditViewer.RTFText := RichText;
end;

procedure CreateAgreementPages();
begin
   // create first agreement page
  FirstAgreementPage := CreateAgreementPage(wpWelcome, SetupMessage(msgWizardLicense),
    'Please read the following Licesense Agreement/Terms of Service. You must accept this terms before continuing with the installation. By accepting this terms and using the app you also accept the YouTube Terms of Service.',
    'license.rtf');

  // create buttons
  FirstAgreementPageAcceptedRadio := CreateRadioButton(FirstAgreementPage.Surface, ScaleY(196), 'I &accept the agreement');
  FirstAgreementPageNotAcceptedRadio := CreateRadioButton(FirstAgreementPage.Surface, ScaleY(216), 'I &do not accept the agreement');

  // initially not accepted
  FirstAgreementPageNotAcceptedRadio.Checked := True;

  // create second agreement page
  SecondAgreementPage := CreateAgreementPage(100, 'Privacy Policy',
    'Please read the following Privacy Policy. You must accept this policy before continuing with the installation. By accepting this privacy policy and using the app you also accept the Google Privacy Policy.',
    'privacy.rtf');

  // create buttons
  SecondAgreementPageAcceptedRadio := CreateRadioButton(SecondAgreementPage.Surface, ScaleY(196), 'I &accept the agreement');
  SecondAgreementPageNotAcceptedRadio := CreateRadioButton(SecondAgreementPage.Surface, ScaleY(216), 'I &do not accept the agreement');

  // initially not accepted
  SecondAgreementPageNotAcceptedRadio.Checked := True;
end;

// event registrations //////////////////////////
function InitializeSetup(): Boolean;
begin
    Result := true;
    if not IsNet6Installed() then
    begin
        MsgBox('.NET 6 ist required to run this app, please download .NET Desktop Runtime from https://dotnet.microsoft.com/en-us/download/dotnet/6.0 and install it.', mbInformation, MB_OK);
        Result := false;
    end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  ResultCode: Integer;
begin
  if (CurStep=ssInstall) then
  begin
    if (IsUpgrade()) then
    begin
      ResultCode := UnInstallOldVersion();
      if(ResultCode <> 3) then
      begin
        MsgBox('Could not uninstall old version. Please remove it manually.', mbInformation, MB_OK);
        Abort();
      end;
    end; 
  end;
end;


procedure CurPageChanged(CurPageID: Integer);
begin
  // Update Next button when user gets to agreement pages
  if (CurPageID = FirstAgreementPage.ID) or (CurPageID = SecondAgreementPage.ID) then
  begin
    WizardForm.NextButton.Enabled := false;
  end;
end;

procedure InitializeWizard();
begin
  CreateAgreementPages();
end;