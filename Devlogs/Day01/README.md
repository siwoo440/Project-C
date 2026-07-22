\# 프로젝트 C 개발 일지



\---



\## 1일차: 프로젝트 기본 구조 구성



\### 개발 목표



Unity 6000.3.91f Universal 2D 환경에서 프로젝트의 기본 구조를 구성하고, 이후 시스템 개발에 사용할 폴더·씬·공용 오브젝트·네이밍 규칙을 확정한다.



\### 개발 환경



| 항목 | 내용 |

|---|---|

| 프로젝트 | Project C |

| 게임명 | ChaosPons |

| 개발 엔진 | Unity 6000.3.91f |

| 프로젝트 형식 | Universal 2D |

| 개발 인원 | 1인 개발 |

| 대상 플랫폼 | Windows PC |

| 버전 관리 | GitHub |



\### 구현 내용



\#### 1. Unity 프로젝트 기본 설정



\- Unity 6000.3.91f 버전 적용

\- Universal 2D 템플릿 적용

\- Company Name 설정

\- Product Name 설정

\- 기본 해상도 1920×1080 설정

\- 초기 화면 모드 Windowed 설정

\- Color Space Linear 설정



\#### 2. Git 호환 설정



\- Asset Serialization을 Force Text로 설정

\- Unity의 `.meta` 파일이 표시되도록 설정

\- Unity 자동 생성 폴더를 제외하는 `.gitignore` 작성



\#### 3. 프로젝트 폴더 구조 구성



\- `Art` 폴더 구성

\- `Audio` 폴더 구성

\- `Data` 폴더 구성

\- `Prefabs` 폴더 구성

\- `Scenes` 폴더 구성

\- `Scripts` 폴더 구성

\- `UI` 폴더 구성

\- 게임 전용 파일을 `Assets/\_ProjectC` 아래로 분리



\#### 4. 주요 씬 생성



| 씬 | 역할 |

|---|---|

| `00\_Boot` | 공용 관리자 초기화 |

| `01\_MainMenu` | 메인 메뉴 |

| `02\_Lobby` | 세룰리온 거점 |

| `03\_Settings` | 환경 설정 |

| `10\_Expedition` | 탑다운 탐사 |

| `20\_Battle` | 카드 전투 |



\#### 5. 기본 Hierarchy 구성



각 씬에 다음 기본 구조를 적용했다.



\- `SceneRoot`

\- `Cameras`

\- `Content`

\- `UI`

\- `Systems`



`00\_Boot` 씬에는 공용 시스템을 관리하기 위한 다음 구조를 추가했다.



\- `AppRoot`

\- `Managers`

\- `Services`



\#### 6. 공용 프리팹 생성



\- `PF\_AppRoot` 프리팹 생성

\- `00\_Boot` 씬에만 `PF\_AppRoot` 배치

\- 이후 공용 관리자 스크립트를 추가할 수 있도록 기본 구조 구성



\#### 7. Build Profiles 설정



\- 주요 씬 6개를 Scene List에 등록

\- `00\_Boot` 씬을 Build Index 0으로 설정

\- 모든 씬의 빌드 포함 상태 확인



\#### 8. 네이밍 규칙 확정



\- 씬 이름에 두 자리 번호 접두사 사용

\- 프리팹에 `PF\_` 접두사 사용

\- ScriptableObject 에셋에 `SO\_` 접두사 사용

\- 스프라이트에 `SPR\_` 접두사 사용

\- 머티리얼에 `MAT\_` 접두사 사용

\- 배경음에 `BGM\_` 접두사 사용

\- 효과음에 `SFX\_` 접두사 사용

\- 코드 파일과 클래스 이름에 PascalCase 사용

\- 지역 변수와 직렬화 필드에 camelCase 사용



\### 테스트 결과



\- 모든 주요 씬 정상 저장 확인

\- 각 씬의 기본 Hierarchy 구성 확인

\- `PF\_AppRoot` 프리팹 연결 확인

\- Build Profiles 씬 순서 확인

\- `00\_Boot` 씬 실행 확인

\- Unity Console 오류 없음 확인



\### 완료 결과



프로젝트 C의 기본 개발 환경과 폴더 구조를 구성했다. 이후 공용 관리자, 씬 전환, 데이터 시스템, 탐사 및 카드 전투 기능을 순차적으로 구현할 수 있는 프로젝트 기반이 준비되었다.



\### 다음 개발 방향



2일차에는 `GameManager`와 `SceneFlowManager`를 구현하고 `PF\_AppRoot`에 연결한다.

