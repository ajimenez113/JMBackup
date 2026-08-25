import { useNavigate, useParams } from 'react-router-dom'
import { Card } from '../../components/Card'
import { Tabs } from '../../components/Tabs'
import { useStrings } from '../../i18n'
import { GeneralTab } from './tabs/GeneralTab'
import { FilesTab } from './tabs/FilesTab'
import { ScheduleTab } from './tabs/ScheduleTab'
import { ExclusionsTab } from './tabs/ExclusionsTab'
import { FiltersTab } from './tabs/FiltersTab'
import { AdvancedTab } from './tabs/AdvancedTab'
import { useTask } from './useTaskGeneral'

export function TaskWizardPage() {
  const strings = useStrings()
  const navigate = useNavigate()
  const params = useParams<{ id?: string }>()
  const routeTaskId = params.id ? Number(params.id) : undefined

  const taskQuery = useTask(routeTaskId)

  // Al crear una tarea nueva, GeneralTab ya avisó con un mensaje de confirmación:
  // acá se vuelve a la lista en vez de quedarse en el asistente, para no dejar al
  // usuario sin saber cómo salir después de guardar (pidió configurar el resto —
  // rutas, horario — editando la tarea de nuevo desde la lista).
  function handleCreated(_id: number) {
    navigate('/', { replace: true })
  }

  const disabledNotice = routeTaskId === undefined ? <p className="text-sm text-fg-muted">{strings.wizard.saveFirstNotice}</p> : null

  return (
    <div className="p-4">
      <Card title={routeTaskId ? strings.wizard.titleEdit : strings.wizard.titleNew}>
        <Tabs
          items={[
            {
              value: 'general',
              label: strings.wizard.tabGeneral,
              content: <GeneralTab taskId={routeTaskId} task={taskQuery.data} onCreated={handleCreated} />,
            },
            {
              value: 'files',
              label: strings.wizard.tabFiles,
              content: routeTaskId !== undefined ? <FilesTab taskId={routeTaskId} /> : disabledNotice,
            },
            {
              value: 'schedule',
              label: strings.wizard.tabSchedule,
              content: routeTaskId !== undefined ? <ScheduleTab taskId={routeTaskId} /> : disabledNotice,
            },
            {
              value: 'exclusions',
              label: strings.wizard.tabExclusions,
              content: routeTaskId !== undefined ? <ExclusionsTab taskId={routeTaskId} /> : disabledNotice,
            },
            {
              value: 'filters',
              label: strings.wizard.tabFilters,
              content: routeTaskId !== undefined ? <FiltersTab taskId={routeTaskId} /> : disabledNotice,
            },
            {
              value: 'advanced',
              label: strings.wizard.tabAdvanced,
              content: routeTaskId !== undefined ? <AdvancedTab taskId={routeTaskId} task={taskQuery.data} /> : disabledNotice,
            },
          ]}
        />
      </Card>
    </div>
  )
}
