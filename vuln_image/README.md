# Как собрать игровой образ

Нужно установить последние версии:

* [VirtualBox](https://www.virtualbox.org/wiki/Downloads)
* [Vagrant](https://www.vagrantup.com/downloads.html)
* [Ansible](http://docs.ansible.com/ansible/latest/intro_installation.html#id12)


В случае ubuntu:

```
sudo add-apt-repository ppa:ansible/ansible
sudo apt-get update
sudo apt install ansible=2.7*
```

В текущем каталоге (`vuln_image`) нужно выполнить команду:

```
vagrant up
```

Скачается последняя версия Ubuntu Xenial (16.04) и выполнится `ansible-playbook setup.yml` (он установит все сервисы).

Для повторного запуска playbook-а нужно выполнить команду

```
vagrant provision
```

Чтобы зайти на образ нужно выполнить команду

```
vagrant ssh
```

Кроме того, можно обойтись без вагранта (именно так и собирался итоговый игровой образ). Устанавливаете Ubuntu 16.04.4 LTS на виртуальную машину,
после чего запускаете на неё конфигурацию с помощью ansible: `ansible-playbook setup.yml`

Роли и скрипты для установки конкретных сервисов смотрите в папке [roles](roles/).
