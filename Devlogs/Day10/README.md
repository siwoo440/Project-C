---
# 10일차 개발일지 - 카드 선택·대상 지정 및 기본 사용 흐름 구축

---
## 개발 목표

9일차에서 구현한 손패 UI를 실제 전투 행동과 연결하여, 카드를 선택하고 대상에게 효과를 적용한 뒤 버린 카드 더미로 이동하는 기본 사용 흐름을 구축했다.

10일차 완료 기준은 다음과 같다.

- 손패 카드 클릭
- 선택 카드 강조와 재클릭 취소
- 카드 대상 규칙 검사
- 선택 가능한 유닛 강조
- 유닛 클릭을 통한 대상 확정
- 기본 피해 효과 적용
- 사용 카드 자동 버리기
- 체력과 손패 UI 자동 갱신
- 사망 소유자와 사망 대상 차단
- 한글 TMP 폰트 경고 해결

---
## 개발 환경

| 항목 | 내용 |
|---|---|
| Engine | Unity 6000.3.21f1 |
| Input | Unity Input System 1.20.0 |
| Battle Script 위치 | `Assets/_ProjectC/Scripts/Battle` |
| Battle Scene | `Assets/_ProjectC/Scenes/40_Battle.unity` |
| Korean Font | Noto Sans CJK KR Regular |

---
## 1. 카드 효과 데이터 추가

카드가 실제 전투 결과를 만들 수 있도록 `CardEffectType`과 효과 수치를 추가했다.

10일차 지원 효과:

- `Damage`: 대상 체력 감소

`CardData`에 효과 종류와 수치를 저장하고, `CardInstance`가 해당 값을 읽을 수 있도록 연결했다.

테스트 공격 카드에는 다음 값을 적용했다.

| 항목 | 값 |
|---|---|
| 대상 | 단일 적 |
| 효과 | 피해 |
| 효과 수치 | 10 |
| 표시 AP 비용 | 1 |

---
## 2. 카드 클릭과 선택 표시

`BattleCardView`에 포인터 클릭 처리를 추가했다.

카드를 클릭하면 해당 `CardInstance`가 손패 화면을 통해 카드 행동 관리자에 전달된다.

선택된 카드는 주황색 배경으로 표시하며, 같은 카드를 다시 클릭하면 선택을 취소한다.

---
## 3. 유닛 클릭과 대상 강조

`BattleUnitView`에 포인터 클릭 이벤트와 대상 가능 표시를 추가했다.

선택 가능한 유닛은 노란색 테두리로 표시한다.

대상 규칙:

- `Self`: 카드 소유자
- `SingleAlly`: 생존 아군 한 명
- `AllAllies`: 모든 생존 아군
- `SingleEnemy`: 생존 적 한 명
- `AllEnemies`: 모든 생존 적

사망한 유닛은 선택 대상에서 제외한다.

---
## 4. BattleCardActionController 구현

카드 선택부터 효과 적용까지 관리하는 `BattleCardActionController`를 추가했다.

담당 기능:

- 현재 선택 카드 관리
- 손패에 존재하는 카드인지 확인
- 카드 소유자의 생존 상태 확인
- 카드 대상 종류 검사
- 단일·전체 대상 목록 생성
- 대상에게 카드 피해 적용
- 사용 카드를 버린 카드 더미로 이동
- 카드와 대상 강조 초기화
- 카드·유닛·덱 이벤트 연결 및 해제

행동 관리자는 프리팹이나 Inspector 연결 없이 `BattleSceneSetup`에서 자동 생성한다.

---
## 5. EventSystem 자동 생성

기존 전투 씬에는 UI 클릭을 처리할 `EventSystem`이 없었다.

전투 초기화 시 기존 EventSystem을 확인하고, 없으면 `EventSystem`과 `InputSystemUIInputModule`을 코드로 자동 생성하도록 구성했다.

Canvas에는 기존 `GraphicRaycaster`가 있으므로 별도의 씬 수정은 필요하지 않다.

---
## 6. 카드 사용 흐름

단일 적 공격 카드의 실행 순서는 다음과 같다.

1. 손패 카드 클릭
2. 카드 선택 색상 적용
3. 생존 적 대상 강조
4. 적 유닛 클릭
5. 대상 규칙 검사
6. 대상에게 10 피해 적용
7. 체력 변경 이벤트 발생
8. 사용 카드를 버린 카드 더미로 이동
9. 덱 상태 변경 이벤트 발생
10. 체력과 손패 UI 자동 갱신

---
## 7. 한글 TMP 폰트 경고 해결

기본 `LiberationSans SDF`에는 한글 글리프가 없어 카드 설명을 표시할 때 다수의 경고가 발생했다.

OFL 라이선스의 `Noto Sans CJK KR Regular`를 프로젝트 리소스로 추가했다.

`ProjectCFontProvider`가 원본 폰트에서 동적 TMP 폰트를 한 번 생성하고 전역 대체 폰트로 등록한다. 카드와 손패에서 생성되는 텍스트도 해당 폰트를 직접 사용한다.

이에 따라 카드 설명의 한글이 사각형 대체 문자 없이 표시된다.

---
## 8. AP 처리 범위

현재 카드에는 `ApCost`가 표시되지만 실제 AP 차감은 적용하지 않았다.

파티 공용 AP인지 캐릭터별 AP인지 규칙이 확정되지 않았으므로, 잘못된 구조를 먼저 고정하지 않고 다음 단계로 분리했다.

---
## 생성·수정·삭제 파일

생성:

- `Assets/_ProjectC/Scripts/Battle/BattleCardActionController.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleCardActionController.cs.meta`
- `Assets/_ProjectC/Scripts/Data/CardEffectType.cs`
- `Assets/_ProjectC/Scripts/Data/CardEffectType.cs.meta`
- `Assets/_ProjectC/Scripts/UI/ProjectCFontProvider.cs`
- `Assets/_ProjectC/Scripts/UI/ProjectCFontProvider.cs.meta`
- `Assets/_ProjectC/Resources.meta`
- `Assets/_ProjectC/Resources/Fonts.meta`
- `Assets/_ProjectC/Resources/Fonts/NotoSansCJKkr-Regular.otf`
- `Assets/_ProjectC/Resources/Fonts/NotoSansCJKkr-Regular.otf.meta`
- `Assets/_ProjectC/Resources/Fonts/NotoSansCJK-OFL.txt`
- `Assets/_ProjectC/Resources/Fonts/NotoSansCJK-OFL.txt.meta`
- `Devlogs/Day10/README.md`

수정:

- `Assets/_ProjectC/Scripts/Data/CardData.cs`
- `Assets/_ProjectC/Scripts/Battle/CardInstance.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleCardView.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleHandView.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleUnitView.cs`
- `Assets/_ProjectC/Scripts/Battle/BattleSceneSetup.cs`
- `Assets/_ProjectC/ScriptableObjects/Cards/Card_TestAttack.asset`

삭제된 파일은 없다.

---
## 검증 결과

- Unity 참조 기반 C# 컴파일 오류 0건
- 컴파일 경고 0건
- Unity Editor에서 카드 클릭과 대상 클릭 실행 확인
- `Test Attack` 피해 10 적용 로그 확인
- 사용 카드의 버린 카드 더미 이동 확인
- Noto Sans CJK KR 폰트 가져오기 성공
- 경고 대상 한글 글리프 포함 확인
- 폰트 적용 이후 한글 글리프 경고 없음
- 신규 메타 GUID 중복 0건
- 신규·수정 코드 한글 주석 누락 0건
- Allman 스타일 위반 0건
- Git 공백 오류 0건

---
## 10일차 결과

손패에 표시된 카드를 클릭하고 유효한 전투 유닛을 대상으로 선택하여 기본 피해를 적용하는 카드 사용 흐름을 완성했다.

피해 적용 이후 기존 체력 이벤트와 덱 상태 이벤트를 이용해 유닛 체력과 손패 화면이 자동으로 갱신된다.

또한 한글 지원 동적 TMP 폰트를 추가하여 카드 설명에서 반복되던 글리프 누락 경고를 해결했다.

---
## 다음 개발 방향

다음 단계에서는 AP 규칙을 확정하고 현재 AP 관리, 카드 비용 검사, AP 차감, 턴 시작 AP 회복을 연결한다. 이후 회복과 보호 효과를 추가하여 카드 효과 실행 구조를 확장한다.

---
## Commit

`10일차 : 카드 선택·대상 지정 및 기본 사용 흐름 구축`
