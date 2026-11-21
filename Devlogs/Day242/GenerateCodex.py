from argparse import ArgumentParser  # 생성 대상 경로 옵션
import json  # 도감 문자 목록 저장
from pathlib import Path  # 유니티 자료 경로 처리
import re  # 직렬화된 원본 참조와 문자열 조회
from uuid import NAMESPACE_URL, uuid5  # 유니티 생성 자산의 고정 식별자
from PIL import Image  # 원본과 분리한 작은 PNG 생성

THUMB_SIZE = 128  # 도감 셀용 최대 픽셀 크기
SCRIPT_GUID = "1b21245badb9576384f781243d5042df"  # 단일 원화 스크립터블 오브젝트 스크립트
REFERENCE = r"\{fileID: (-?\d+), guid: ([a-f0-9]+), type: \d+\}"  # 유니티 파일 식별자와 참조 키
QUOTED = r'"(?:[^"\\]|\\.)*"'  # 줄바꿈 이스케이프를 허용한 문자열
RESOURCE_RELATIVE = Path("Assets/_ProjectC/Resources/Codex")  # 도감 전용 리소스 위치


def read(path):  # UTF8 에셋과 C# 텍스트 읽기
    return path.read_text(encoding="utf-8-sig")  # 선택적 BOM 제거


def write(path, value):  # 고정 줄 끝의 생성 자료 쓰기
    path.parent.mkdir(parents=True, exist_ok=True)  # 출력 폴더 준비
    path.write_text(value, encoding="utf-8", newline="\n")  # 동일한 줄 끝과 문자 인코딩 유지


def guid(path):  # 출력 경로에서 불변 식별자 만들기
    return uuid5(NAMESPACE_URL, "project-c/codex/" + path.as_posix()).hex  # 재생성 시 같은 GUID 반환


def fields(text):  # 유니티 YAML의 최상위 멤버 분리
    pieces = re.split(r"^  ([A-Za-z_]\w*):[ \t]*", text, flags=re.M)  # 다른 객체의 하위 필드와 분리
    return dict(zip(pieces[1::2], pieces[2::2]))  # 필드별 줄과 리스트 문자열 대응


def scalar(value):  # 현재 카탈로그 문자열의 YAML 접기와 이스케이프 해석
    value = re.sub(r"\r?\n[ \t]+", " ", value.strip())  # 유니티가 줄 길이로 접은 문장 복원
    if not value or value == "null":  # 실제 빈 문자열 확인
        return ""  # 런타임 기본 설명 유지
    if value.startswith('"'):  # 유니티 큰따옴표 문자열 확인
        value = re.sub(r"\\x([0-9a-fA-F]{2})", r"\\u00\1", value)  # 유니티 문자 이스케이프의 JSON 표기 변환
        return json.loads(value)  # 줄바꿈과 유니코드 문자 해석
    if value.startswith("'"):  # YAML 작은따옴표 문자열 확인
        return value[1:-1].replace("''", "'")  # YAML 따옴표 이스케이프 처리
    if value[0] in "|>":  # 미지원 블록 형식 감지
        raise ValueError("Unsupported YAML scalar: " + value)  # 원본 형식 변화에서 조용한 누락 차단
    return value  # 따옴표 없는 원본 이름 또는 문구 반환


def source_index(project):  # 에셋 GUID의 실제 파일 경로 조회
    index = {}  # 프로젝트 원본들의 경로 보관
    for meta in (project / "Assets").rglob("*.meta"):  # 씬에서 참조할 원본 메타 순회
        match = re.search(r"^guid:\s+([a-f0-9]{32})", read(meta), re.M)  # 메타 파일의 최상위 GUID 조회
        if match:  # 유효한 원본 식별자 확인
            index[match[1]] = meta.with_suffix("")  # 메타 대응 자산 위치 등록
    return index  # 전체 원본 참조 색인 반환


def collect_rows(project, sources):  # 게임과 같은 자료에서 도감 항목 수집
    project_assets = project / "Assets/_ProjectC"  # 게임이 관리하는 실제 자료 루트
    resources = project_assets / "Resources"  # 이벤트 그림과 카탈로그 위치
    catalog = fields(read(resources / "Save/SaveAssetCatalog.asset"))  # 저장 가능한 카드와 아이템 원본 목록
    original_art = {}  # 중복 사용 원본의 식별자와 실제 경로
    rows = []  # 분류별로 원본 순서를 유지한 도감 목록

    def add(category, name, desc, reference=None):  # 문자열과 원화 주소 한 줄 추가
        artwork_key = ""  # 원화가 없는 항목의 빈 키
        if reference:  # 에셋에서 연결한 원화 존재 확인
            file_id, asset_guid = reference  # 단일 스프라이트를 가리키는 참조 분리
            artwork_key = asset_guid if file_id == "21300000" else f"{asset_guid}_{file_id}"  # 같은 이미지 안의 개별 스프라이트 구분
            original_art[artwork_key] = (sources[asset_guid], asset_guid, file_id)  # 별도 원화 래퍼 생성에 필요한 원본 정보
        rows.append({"category": category, "name": name, "description": desc, "artworkKey": artwork_key})  # 이미지 객체 없는 도감 행 저장

    def add_asset(category, data, reference_field):  # 일반 스크립터블 오브젝트 이름과 그림 변환
        reference = re.fullmatch(REFERENCE, data.get(reference_field, "").strip())  # 실제 스프라이트 연결 확인
        add(category, scalar(data["displayName"]), scalar(data.get("description", "")), reference.groups() if reference else None)  # 도감용 이름과 설명 추출

    exploration = project_assets / "Scripts/Exploration"  # 런타임 생성 이벤트 원본 위치
    event_catalog = read(exploration / "ExplorationEventCatalog.cs")  # 현재 버전까지 포함하는 실제 탐사 목록
    events = {}  # 기존과 추가 이벤트를 실제 식별자로 정렬할 목록
    authored = list((resources / "ExplorationEvents").glob("*.asset"))  # 원본 테스트 사건을 대체하는 에셋 확인
    for path in authored:  # 현재 프로젝트의 기존 원본 사건 순회
        data = fields(read(path))  # 에셋 사건 이름과 설명 조회
        events[scalar(data["eventId"])] = (scalar(data["displayName"]), scalar(data["description"]), None)  # 에셋 사건의 도감 문자 보관
        reference = re.fullmatch(REFERENCE, data.get("illustrationSprite", "").strip())  # 에셋 사건의 원화 참조 확인
        if reference:  # 원화가 있는 원본 사건 확인
            events[scalar(data["eventId"])] = (*events[scalar(data["eventId"])][:2], reference.groups())  # 실제 일러스트 참조 연결
    if not authored:  # 실제 코드의 초기 이벤트 폴백 처리
        fallback = r"eventData\.Initialize\(\s*(" + QUOTED + r")\s*,\s*(" + QUOTED + r")\s*,\s*(" + QUOTED + r")"  # 원본 사건 초기화에서 고정 문구를 읽는 형식
        for ident, name, desc in re.findall(fallback, event_catalog):  # 실제 폴백 사건의 이름과 설명 순회
            events[json.loads(ident)] = (json.loads(name), json.loads(desc), None)  # 원화 없는 초기 사건 기록
    if not events:  # 초기 이벤트 추출 유효성 확인
        raise ValueError("No original exploration events")  # 탐사 코드 형식 변경 알림
    for event_type in dict.fromkeys(re.findall(r"\.Add\((\w+Event)\.Create\(\)\)", event_catalog)):  # 현재 탐사 목록에 추가되는 코드형 사건 순회
        code = read(exploration / (event_type + ".cs"))  # 사건 초기화의 실제 표시 문구와 원화 조회
        ident = re.search(r"\bId\s*=\s*(" + QUOTED + r")", code)[1]  # 런타임에서 사용하는 사건 ID
        body = re.search(r"\.Initialize\(Id,\s*(" + QUOTED + r"),\s*(" + QUOTED + r").*?Resources\.Load<Sprite>\((" + QUOTED + r")\)", code, re.S)  # 포맷된 사건 본문과 일러스트 위치
        art_path = resources / (json.loads(body[3]) + ".png")  # 리소스 주소의 실제 원화 파일
        art_guid = re.search(r"^guid:\s+([a-f0-9]{32})", read(Path(str(art_path) + ".meta")), re.M)[1]  # 이벤트 원화의 유니티 참조 키
        events[json.loads(ident)] = (json.loads(body[1]), json.loads(body[2]), ("21300000", art_guid))  # 코드형 사건의 실제 내용 보관
    for name, desc, source in (events[key] for key in sorted(events)):  # 실제 탐사 ID 순서의 사건 자료
        add(0, name, desc, source)  # 상호작용 도감 행 생성

    for _, key in re.findall(REFERENCE, catalog["consumables"]):  # 저장 카탈로그의 소모품 목록 순회
        data = fields(read(sources[key]))  # 저장 가능한 소모품 실제 원본
        if data.get("m_EditorClassIdentifier", "").strip().endswith("::PotionData") or "potionEffectType" in data:  # 포션 자료형 확인
            add_asset(1, data, "icon")  # 포션 도감 행 추가
    seen = set()  # 여러 조우에서 사용하는 같은 적 제외
    for encounter in sorted((resources / "Encounters").glob("*.asset")):  # 도감에 표시된 원본 조우 범위 유지
        for _, key in re.findall(REFERENCE, fields(read(encounter))["enemies"]):  # 조우별 적 데이터 참조 순회
            data = fields(read(sources[key]))  # 실제 적 원본 조회
            ident = scalar(data["enemyId"])  # 중복 판정에 사용할 적 식별자
            if ident not in seen:  # 같은 적 중복 제거
                seen.add(ident)  # 현재 적의 등록 기록
                add_asset(2, data, "portrait")  # 적 도감의 이름과 초상화 원본
    for _, key in re.findall(REFERENCE, catalog["relics"]):  # 저장 가능한 유물의 원본 순서 유지
        add_asset(3, fields(read(sources[key])), "icon")  # 유물 도감 행 생성
    for _, key in re.findall(REFERENCE, catalog["cards"]):  # 실제 보상과 저장 복원 카드 원본 순회
        add_asset(7, fields(read(sources[key])), "artwork")  # 카드 도감 행 생성
    return rows, original_art  # 도감 문자와 원화의 분리된 출력 자료


def save_thumbnail(output, key, image):  # 원본과 별도인 작은 스프라이트 생성
    target = RESOURCE_RELATIVE / "Thumbnails" / f"CodexThumb_{key}.png"  # 해시로 찾는 작은 그림 경로
    image = image.convert("RGBA")  # 알파를 보존한 원본 픽셀 변환
    image.thumbnail((THUMB_SIZE, THUMB_SIZE), Image.Resampling.LANCZOS)  # 원본 비율을 유지한 저해상도 이미지
    (output / target).parent.mkdir(parents=True, exist_ok=True)  # 썸네일 출력 폴더 준비
    image.save(output / target, optimize=True)  # 독립 PNG 파일 저장
    write(output / Path(str(target) + ".meta"), f"fileFormatVersion: 2\nguid: {guid(target)}\nTextureImporter:\n  serializedVersion: 13\n  mipmaps:\n    enableMipMap: 0\n    sRGBTexture: 1\n  isReadable: 0\n  maxTextureSize: {THUMB_SIZE}\n  nPOTScale: 0\n  spriteMode: 1\n  spritePixelsToUnits: 100\n  alphaIsTransparency: 1\n  textureType: 8\n  textureShape: 1\n")  # 낮은 해상도와 독립 스프라이트 임포트 설정


def crop_sprite(path, file_id, image):  # 필요한 경우 원본 텍스처에서 선택한 스프라이트 자르기
    if file_id == "21300000":  # 단일 스프라이트인 현재 원화 확인
        return image  # 이미지 전체 반환
    meta = read(Path(str(path) + ".meta"))  # 부분 스프라이트의 좌표를 담은 메타 데이터
    for record in re.split(r"(?m)^    - serializedVersion:", meta)[1:]:  # 스프라이트별 개별 기록 순회
        if not re.search(r"(?m)^      internalID: " + re.escape(file_id) + r"\s*$", record):  # 참조의 파일 ID와 비교
            continue  # 다른 스프라이트 제외
        rect = re.search(r"rect:\s+(?:serializedVersion: \d+\s+)?x: (\d+)\s+y: (\d+)\s+width: (\d+)\s+height: (\d+)", record)  # 실제 메타 지정 스프라이트 사각형
        x, y, w, h = map(int, rect.groups())  # 좌하 원점의 유니티 좌표 해석
        return image.crop((x, image.height - y - h, x + w, image.height - y))  # 위쪽 원점으로 변환해 올바른 원화 영역 사용
    raise ValueError(f"Missing sprite {file_id} in {path}")  # 미확인 좌표의 잘못된 썸네일 생성 차단


def save_original_reference(output, key, asset_guid, file_id):  # 기존 게임 원화 한 장만 참조하는 리소스 생성
    target = RESOURCE_RELATIVE / "Originals" / f"{key}.asset"  # 원화에 해당하는 한 개의 주소
    body = "%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n--- !u!114 &11400000\nMonoBehaviour:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n  m_GameObject: {fileID: 0}\n  m_Enabled: 1\n  m_EditorHideFlags: 0\n"  # 유니티 단일 스크립터블 오브젝트 헤더
    body += f"  m_Script: {{fileID: 11500000, guid: {SCRIPT_GUID}, type: 3}}\n  m_Name: {key}\n  m_EditorClassIdentifier: Assembly-CSharp::CodexArtworkResource\n  artwork: {{fileID: {file_id}, guid: {asset_guid}, type: 3}}\n"  # 다른 도감 항목 연결 없이 기존 원화만 참조
    write(output / target, body)  # 원화 자체를 복제하지 않는 작은 에셋 저장
    write(output / Path(str(target) + ".meta"), f"fileFormatVersion: 2\nguid: {guid(target)}\nNativeFormatImporter:\n  mainObjectFileID: 11400000\n")  # 원화 참조 에셋 고정 메타


def save_status_thumbnails(project, output):  # 원화 접근 전 사용할 상태 아이콘 썸네일 생성
    scripts = project / "Assets/_ProjectC/Scripts"  # 상태 정의와 이미지 매핑의 실제 루트
    icon_dir = project / "Assets/_ProjectC/Resources/UI/Battle"  # 개별 및 공용 상태 원화 실제 위치
    art = read(scripts / "Battle/BattleStatusIconArt.cs")  # 실제 상태 아이콘 선택 규칙 조회
    enum = read(scripts / "Data/BattleStatusEffectType.cs")  # 현재 배정된 효과 종류 조회
    kinds = re.findall(r"^\s{4}(\w+)(?:\s*=\s*\d+)?,?\s*//", enum, re.M)  # 실제 상태 열거형 멤버 이름
    individual = set(re.findall(r"type == BattleStatusEffectType\.(\w+)", art))  # 개별 원화가 지정된 상태
    indices = dict(re.findall(r"case BattleStatusEffectType\.(\w+): return (\d+);", art))  # 기본 공용 타일 이미지의 순번
    default = int(re.search(r"default: return (\d+);", art)[1])  # 대응 상태가 없는 경우의 기본 타일
    with Image.open(icon_dir / "StatusIcons.png") as sheet:  # 기존 전투 상태 스프라이트 시트 읽기
        tile = sheet.width // 4  # 게임과 같은 네 열의 타일 크기
        for kind in kinds:  # 실제 등록 상태 이미지 순회
            if kind in individual:  # 독립 그림을 표시하는 상태 확인
                with Image.open(icon_dir / f"{kind}StatusIcon.png") as icon:  # 실제 전투에서 쓰는 원본 한 장 읽기
                    save_thumbnail(output, "status_" + kind, icon)  # 독립적인 저해상도 이미지 생성
            else:  # 기존 공용 시트에서 상태를 읽는 경우
                index = int(indices.get(kind, default))  # 상태별 공용 타일 위치
                x, y = (index % 4) * tile, (index // 4) * tile  # 공용 타일의 원본 픽셀 좌표
                save_thumbnail(output, "status_" + kind, sheet.crop((x, y, x + tile, y + tile)))  # 전체 시트 없이 표시할 개별 썸네일


def generate(project, output):  # 프로젝트 원본을 도감 텍스트와 이미지로 분리 생성
    rows, references = collect_rows(project, source_index(project))  # 게임 데이터와 에셋 주소 추출
    if {row["category"] for row in rows} != {0, 1, 2, 3, 7}:  # 비어 있으면 안 되는 분류의 추출 확인
        raise ValueError("Incomplete Codex category extraction")  # 자료가 일부 누락된 출력 방지
    for key, (source, asset_guid, file_id) in sorted(references.items()):  # 한 원본당 한 쌍의 개별 자산 생성
        with Image.open(source) as image:  # 실제 원본의 픽셀만 읽기
            save_thumbnail(output, key, crop_sprite(source, file_id, image))  # 작은 단일 그림 생성
        save_original_reference(output, key, asset_guid, file_id)  # 상세 창에서만 읽을 원래 스프라이트 주소 생성
    save_status_thumbnails(project, output)  # 원본 시트와 개별 원화와 분리한 상태 썸네일 생성
    index_path = RESOURCE_RELATIVE / "Index.json"  # 게임 전체 원화와 분리된 문자 색인 위치
    write(output / index_path, json.dumps({"rows": rows}, ensure_ascii=False, indent=2) + "\n")  # 원본 에셋 없는 도감 목록 저장
    write(output / Path(str(index_path) + ".meta"), f"fileFormatVersion: 2\nguid: {guid(index_path)}\nTextScriptImporter:\n  externalObjects: {{}}\n")  # JSON 텍스트 에셋의 고정 메타
    for directory in (RESOURCE_RELATIVE, RESOURCE_RELATIVE / "Originals", RESOURCE_RELATIVE / "Thumbnails"):  # 생성 자산 폴더 순회
        write(output / Path(str(directory) + ".meta"), f"fileFormatVersion: 2\nguid: {guid(directory)}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {{}}\n")  # 생성 자산 폴더의 고정 메타
    counts = {kind: sum(row["category"] == kind for row in rows) for kind in (0, 1, 2, 3, 7)}  # 분류별 원본 수량 확인
    print(f"Codex rows {counts}; separate sources {len(references)}")  # 생성된 메타데이터와 원화 주소 수량 표시


if __name__ == "__main__":  # 수동 생성 명령 진입점
    parser = ArgumentParser()  # 경로 인자 이름 준비
    parser.add_argument("--project", type=Path, required=True)  # 실제 게임 원본이 있는 프로젝트
    parser.add_argument("--output", type=Path, required=True)  # 생성 자료를 적용할 프로젝트 또는 초안 루트
    args = parser.parse_args()  # 명령의 입력 경로 확인
    generate(args.project, args.output)  # 독립 도감 자료 생성
