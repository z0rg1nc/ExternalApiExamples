Usage example:

```
(ExternalSolverExample.exe|run_on_mono.sh) --td=__TASK_DESCRIPTION__ --outdir="__SOLUTION_DIR__" --thread-count=2 --native-support=0
```

native-support: 0-managed, 1-windows dll, 2-linux so

for Scrypt only

Dependencies:
* BtmGeneralClientInterfacesLib
* MiscUtilsLib
* ComputableTaskSolversLib